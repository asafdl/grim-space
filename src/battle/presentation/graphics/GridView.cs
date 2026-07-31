using System;
using System.Collections.Generic;
using Godot;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class GridView : Node3D
{
	[Export] public int DotPaddingCells { get; set; } = 1;
	[Export] public int DotStride { get; set; } = 5;
	[Export] public float DotPixelRadius { get; set; } = 4f;
	[Export] public Color DotColor { get; set; } = new(0.4f, 0.58f, 0.78f, 0.4f);

	private const string DotShaderPath = "res://src/battle/presentation/graphics/grid_dot.gdshader";

	private BoundedGrid? _grid;
	private GridDotRegion _dotRegion;
	private MultiMeshInstance3D? _dotField;
	private Camera3D? _dotCamera;
	private GridDotPlacementMode _dotPlacementMode = GridDotPlacementMode.Volume;
	private GridDotSliceAxis _activeSliceAxis = GridDotSliceAxis.Z;
	private readonly Dictionary<Coord, MeshInstance3D> _highlights = new();

	public event Action<string?>? DotPlaneLabelChanged;

	private StandardMaterial3D? _defaultMaterial;
	private StandardMaterial3D? _endpoint3Ap;
	private StandardMaterial3D? _endpoint4Ap;
	private StandardMaterial3D? _pathMaterial;
	private StandardMaterial3D? _hoverMaterial;
	private StandardMaterial3D? _hazardMaterial;
	private StandardMaterial3D? _targetMaterial;
	private StandardMaterial3D? _railgunMaterial;
	private StandardMaterial3D? _aimMaterial;
	private StandardMaterial3D? _flakPortMaterial;
	private StandardMaterial3D? _flakStarboardMaterial;
	private StandardMaterial3D? _flakPreviewMaterial;

	public void Build(BoundedGrid grid, GridDotRegion region)
	{
		_grid = grid;
		_dotRegion = region;
		ClearHighlightMeshes();
		RebuildDotField(region);

		_defaultMaterial = CreateMaterial(new Color(0.35f, 0.65f, 0.95f, 0.42f));
		_endpoint3Ap = _defaultMaterial;
		_endpoint4Ap = CreateMaterial(new Color(0.12f, 0.28f, 0.62f, 0.58f));
		_pathMaterial = CreateMaterial(new Color(0.45f, 0.5f, 0.6f, 0.22f));
		_hoverMaterial = CreateMaterial(new Color(0.95f, 0.95f, 1f, 0.65f));
		_hazardMaterial = CreateMaterial(new Color(0.95f, 0.25f, 0.15f, 0.55f));
		_targetMaterial = CreateMaterial(new Color(0.95f, 0.85f, 0.2f, 0.55f));
		_railgunMaterial = CreateMaterial(new Color(0.85f, 0.35f, 1f, 0.65f));
		_aimMaterial = CreateMaterial(new Color(0.35f, 0.8f, 1f, 0.45f));
		_flakPortMaterial = CreateMaterial(new Color(0.9f, 0.55f, 0.15f, 0.5f));
		_flakStarboardMaterial = CreateMaterial(new Color(0.95f, 0.75f, 0.2f, 0.5f));
		_flakPreviewMaterial = CreateMaterial(new Color(1f, 0.85f, 0.25f, 0.7f));
	}

	public void SetDotStride(int stride)
	{
		DotStride = System.Math.Max(1, stride);
		RebuildDotField(_dotRegion);
	}

	public void SetDotCamera(Camera3D camera) => _dotCamera = camera;

	public void SetDotPlacementMode(GridDotPlacementMode mode)
	{
		_dotPlacementMode = mode;
		RebuildDotField(_dotRegion);
	}

	public override void _Process(double _)
	{
		if (_dotPlacementMode != GridDotPlacementMode.CameraPlane || _dotCamera is null)
			return;

		var axis = GridDotCameraPlane.ClosestAxisAlignedPlane(_dotCamera);
		if (axis == _activeSliceAxis)
			return;

		_activeSliceAxis = axis;
		RebuildDotField(_dotRegion);
	}

	public void ClearHighlights() => ClearHighlightMeshes();

	public void SetMoveHighlights(
		IReadOnlyList<Option> options,
		IReadOnlyList<Coord> path,
		Coord? target,
		IReadOnlySet<Coord>? hazardCells = null)
	{
		if (!EnsureMaterials())
			return;

		ClearHighlightMeshes();

		var endpointAp = new Dictionary<Coord, int>();
		foreach (var option in options)
			endpointAp[option.EndPosition] = option.ApCost;

		var pathSet = new HashSet<Coord>(path);

		foreach (var (coord, ap) in endpointAp)
		{
			if (pathSet.Contains(coord) || coord == target)
				continue;

			SetCellMaterial(coord, ap == 3 ? _endpoint3Ap! : _endpoint4Ap!);
		}

		foreach (var coord in pathSet)
			SetCellMaterial(coord, _pathMaterial!);

		if (target is Coord hovered)
			SetCellMaterial(hovered, _hoverMaterial!);

		if (hazardCells is not null)
		{
			foreach (var coord in hazardCells)
				SetCellMaterial(coord, _hazardMaterial!);
		}
	}

	public void SetMissileHighlights(
		IReadOnlySet<Coord> hazardCells,
		IReadOnlySet<Coord> validCells,
		IReadOnlySet<Coord> previewCells)
	{
		if (!EnsureMaterials())
			return;

		ClearHighlightMeshes();

		foreach (var coord in hazardCells)
			SetCellMaterial(coord, _hazardMaterial!);

		foreach (var coord in validCells)
		{
			if (previewCells.Contains(coord))
				continue;

			SetCellMaterial(coord, _aimMaterial!);
		}

		foreach (var coord in previewCells)
			SetCellMaterial(coord, _targetMaterial!);
	}

	public void SetRailgunHighlights(
		IReadOnlySet<Coord> targetCells,
		Coord? hoveredCell,
		IReadOnlySet<Coord>? hazardCells = null)
	{
		if (!EnsureMaterials())
			return;

		ClearHighlightMeshes();

		if (hazardCells is not null)
		{
			foreach (var coord in hazardCells)
				SetCellMaterial(coord, _hazardMaterial!);
		}

		foreach (var coord in targetCells)
			SetCellMaterial(coord, _railgunMaterial!);

		if (hoveredCell is Coord hovered && targetCells.Contains(hovered))
			SetCellMaterial(hovered, _hoverMaterial!);
	}

	public void SetFlakHighlights(
		IReadOnlySet<Coord> hazardCells,
		IReadOnlySet<Coord> portCells,
		IReadOnlySet<Coord> starboardCells,
		IReadOnlySet<Coord> previewCells)
	{
		if (!EnsureMaterials())
			return;

		ClearHighlightMeshes();

		foreach (var coord in hazardCells)
			SetCellMaterial(coord, _hazardMaterial!);

		foreach (var coord in portCells)
		{
			if (previewCells.Contains(coord))
				continue;

			SetCellMaterial(coord, _flakPortMaterial!);
		}

		foreach (var coord in starboardCells)
		{
			if (previewCells.Contains(coord))
				continue;

			SetCellMaterial(coord, _flakStarboardMaterial!);
		}

		foreach (var coord in previewCells)
			SetCellMaterial(coord, _flakPreviewMaterial!);
	}

	private bool EnsureMaterials() =>
		_grid is not null
		&& _defaultMaterial is not null
		&& _endpoint4Ap is not null
		&& _pathMaterial is not null
		&& _hoverMaterial is not null
		&& _hazardMaterial is not null
		&& _targetMaterial is not null
		&& _railgunMaterial is not null
		&& _aimMaterial is not null
		&& _flakPortMaterial is not null
		&& _flakStarboardMaterial is not null
		&& _flakPreviewMaterial is not null;

	private void SetCellMaterial(Coord coord, StandardMaterial3D material)
	{
		if (_highlights.TryGetValue(coord, out var existing))
		{
			existing.MaterialOverride = material;
			return;
		}

		var cell = new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = Vector3.One * PointMapping.CellSize * 0.92f },
			Position = PointMapping.ToWorld(coord),
			MaterialOverride = material,
		};
		AddChild(cell);
		_highlights[coord] = cell;
	}

	private static StandardMaterial3D CreateMaterial(Color color) =>
		new()
		{
			AlbedoColor = color,
			Roughness = 0.9f,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
		};

	private void ClearHighlightMeshes()
	{
		foreach (var child in _highlights.Values)
			child.Free();

		_highlights.Clear();
	}

	private void RebuildDotField(GridDotRegion region)
	{
		_dotField?.QueueFree();
		_dotField = null;

		var stride = System.Math.Max(1, DotStride);
		var sliceAxis = ResolveSliceAxis();
		var count = CountDotInstances(region, stride, sliceAxis);
		if (count <= 0)
		{
			NotifyPlaneLabel(sliceAxis);
			return;
		}

		var multiMesh = new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			InstanceCount = count,
			Mesh = new QuadMesh { Size = Vector2.One },
		};

		var index = 0;
		foreach (var coord in EnumerateDotCoords(region, stride, sliceAxis))
		{
			multiMesh.SetInstanceTransform(
				index,
				new Transform3D(Basis.Identity, PointMapping.ToWorld(coord)));
			index++;
		}

		var dotShader = GD.Load<Shader>(DotShaderPath);
		var dotMaterial = new ShaderMaterial
		{
			Shader = dotShader,
			RenderPriority = -1,
		};
		dotMaterial.SetShaderParameter("albedo", DotColor);
		dotMaterial.SetShaderParameter("pixel_radius", DotPixelRadius);

		_dotField = new MultiMeshInstance3D
		{
			Name = "DotField",
			Multimesh = multiMesh,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = dotMaterial,
		};
		AddChild(_dotField);
		MoveChild(_dotField, 0);
		NotifyPlaneLabel(sliceAxis);
	}

	private GridDotSliceAxis? ResolveSliceAxis()
	{
		if (_dotPlacementMode != GridDotPlacementMode.CameraPlane || _dotCamera is null)
			return null;

		_activeSliceAxis = GridDotCameraPlane.ClosestAxisAlignedPlane(_dotCamera);
		return _activeSliceAxis;
	}

	private void NotifyPlaneLabel(GridDotSliceAxis? sliceAxis)
	{
		if (_dotPlacementMode != GridDotPlacementMode.CameraPlane || sliceAxis is not GridDotSliceAxis axis)
		{
			DotPlaneLabelChanged?.Invoke(null);
			return;
		}

		DotPlaneLabelChanged?.Invoke(GridDotCameraPlane.PlaneLabel(axis));
	}

	private static int SliceCoord(GridDotRegion region, GridDotSliceAxis axis) =>
		axis switch
		{
			GridDotSliceAxis.X => (region.MinX + region.MaxX) / 2,
			GridDotSliceAxis.Y => (region.MinY + region.MaxY) / 2,
			GridDotSliceAxis.Z => (region.MinZ + region.MaxZ) / 2,
			_ => 0,
		};

	private static int FirstAlignedCoord(int min, int stride)
	{
		if (stride <= 1)
			return min;

		var remainder = min % stride;
		return remainder == 0 ? min : min + (stride - remainder);
	}

	private static int CountDotInstances(GridDotRegion region, int stride, GridDotSliceAxis? sliceAxis)
	{
		var count = 0;
		foreach (var _ in EnumerateDotCoords(region, stride, sliceAxis))
			count++;

		return count;
	}

	private static IEnumerable<Coord> EnumerateDotCoords(
		GridDotRegion region,
		int stride,
		GridDotSliceAxis? sliceAxis)
	{
		if (sliceAxis is GridDotSliceAxis axis)
		{
			var slice = SliceCoord(region, axis);
			slice = System.Math.Clamp(slice, SliceMin(region, axis), SliceMax(region, axis));

			foreach (var coord in axis switch
			{
				GridDotSliceAxis.X => EnumerateYzPlane(region, stride, slice),
				GridDotSliceAxis.Y => EnumerateXzPlane(region, stride, slice),
				GridDotSliceAxis.Z => EnumerateXyPlane(region, stride, slice),
				_ => Array.Empty<Coord>(),
			})
				yield return coord;

			yield break;
		}

		var startX = FirstAlignedCoord(region.MinX, stride);
		var startY = FirstAlignedCoord(region.MinY, stride);
		var startZ = FirstAlignedCoord(region.MinZ, stride);

		for (var x = startX; x <= region.MaxX; x += stride)
		{
			for (var y = startY; y <= region.MaxY; y += stride)
			{
				for (var z = startZ; z <= region.MaxZ; z += stride)
					yield return new Coord(x, y, z);
			}
		}
	}

	private static int SliceMin(GridDotRegion region, GridDotSliceAxis axis) =>
		axis switch
		{
			GridDotSliceAxis.X => region.MinX,
			GridDotSliceAxis.Y => region.MinY,
			GridDotSliceAxis.Z => region.MinZ,
			_ => 0,
		};

	private static int SliceMax(GridDotRegion region, GridDotSliceAxis axis) =>
		axis switch
		{
			GridDotSliceAxis.X => region.MaxX,
			GridDotSliceAxis.Y => region.MaxY,
			GridDotSliceAxis.Z => region.MaxZ,
			_ => 0,
		};

	private static IEnumerable<Coord> EnumerateXyPlane(GridDotRegion region, int stride, int z)
	{
		var startX = FirstAlignedCoord(region.MinX, stride);
		var startY = FirstAlignedCoord(region.MinY, stride);
		for (var x = startX; x <= region.MaxX; x += stride)
		{
			for (var y = startY; y <= region.MaxY; y += stride)
				yield return new Coord(x, y, z);
		}
	}

	private static IEnumerable<Coord> EnumerateXzPlane(GridDotRegion region, int stride, int y)
	{
		var startX = FirstAlignedCoord(region.MinX, stride);
		var startZ = FirstAlignedCoord(region.MinZ, stride);
		for (var x = startX; x <= region.MaxX; x += stride)
		{
			for (var z = startZ; z <= region.MaxZ; z += stride)
				yield return new Coord(x, y, z);
		}
	}

	private static IEnumerable<Coord> EnumerateYzPlane(GridDotRegion region, int stride, int x)
	{
		var startY = FirstAlignedCoord(region.MinY, stride);
		var startZ = FirstAlignedCoord(region.MinZ, stride);
		for (var y = startY; y <= region.MaxY; y += stride)
		{
			for (var z = startZ; z <= region.MaxZ; z += stride)
				yield return new Coord(x, y, z);
		}
	}
}
