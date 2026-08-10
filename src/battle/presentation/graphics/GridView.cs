using System.Collections.Generic;
using Godot;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class GridView : Node3D
{
	private const float HighlightCellScale = 0.8f;
	private const float EndpointScaleLowAp = 0.85f;
	private const float EndpointScaleMidAp = 0.7f;
	private const float EndpointScaleHighAp = 0.55f;

	private BoundedGrid? _grid;
	private readonly Dictionary<Coord, MeshInstance3D> _activeHighlights = new();
	private readonly Queue<MeshInstance3D> _freeHighlights = new();

	private StandardMaterial3D? _endpointApLow;
	private StandardMaterial3D? _endpointApMid;
	private StandardMaterial3D? _endpointApHigh;
	private StandardMaterial3D? _pathMaterial;
	private StandardMaterial3D? _hoverMaterial;
	private StandardMaterial3D? _targetMaterial;

	public void Build(BoundedGrid grid)
	{
		_grid = grid;
		ReleaseActiveHighlights();

		// Categorical AP: ≤2 sage-green, 3 clear blue, ≥4 deep indigo — hues spaced apart.
		_endpointApLow = CreateMaterial(new Color(0.38f, 0.62f, 0.45f, 0.52f));
		_endpointApMid = CreateMaterial(new Color(0.32f, 0.48f, 0.78f, 0.54f));
		_endpointApHigh = CreateMaterial(new Color(0.28f, 0.34f, 0.52f, 0.56f));
		_pathMaterial = CreateMaterial(new Color(0.45f, 0.5f, 0.6f, 0.22f));
		_hoverMaterial = CreateMaterial(new Color(0.95f, 0.95f, 1f, 0.65f));
		_targetMaterial = CreateMaterial(new Color(0.95f, 0.85f, 0.2f, 0.55f));
	}

	public void ApplyFrame(PresentationFrame frame)
	{
		if (!frame.ShowMovePreview || frame.ShowOutcomeOverlay)
		{
			ReleaseActiveHighlights();
			PresentationDiagnostics.LogMovePreviewHighlights(0, 0);
			return;
		}

		switch (frame.Mode)
		{
			case EPlayerMode.Move:
				SetMoveHighlights(
					frame.MovePaths,
					frame.MovePathApBaseline,
					frame.MovePath,
					frame.MoveTarget);
				break;

			case EPlayerMode.Flak:
			case EPlayerMode.Railgun:
			case EPlayerMode.Torpedo:
				// Weapon volumes are drawn by *PreviewView; keep cells for picking only.
				ReleaseActiveHighlights();
				break;
		}
	}

	public void SetMoveHighlights(
		IReadOnlyList<MovePathSession> paths,
		int pathApBaseline,
		IReadOnlyList<Coord> path,
		Coord? target)
	{
		if (!EnsureMaterials())
			return;

		ReleaseActiveHighlights();

		var endpointAp = new Dictionary<Coord, int>();
		foreach (var session in paths)
			endpointAp[session.EndPosition] = session.ExtensionApCost(pathApBaseline);

		var pathSet = new HashSet<Coord>(path);

		PresentationDiagnostics.LogMovePreviewHighlights(paths.Count, endpointAp.Count);

		foreach (var coord in pathSet)
		{
			if (coord == target)
				continue;

			SetCellHighlight(coord, _pathMaterial!);
		}

		foreach (var (coord, ap) in endpointAp)
		{
			if (coord == target)
				continue;

			SetCellHighlight(coord, EndpointMaterialForAp(ap), EndpointScaleForAp(ap));
		}

		if (target is Coord hovered)
			SetCellHighlight(hovered, _hoverMaterial!);
	}

	private bool EnsureMaterials() =>
		_grid is not null
		&& _endpointApLow is not null
		&& _endpointApMid is not null
		&& _endpointApHigh is not null
		&& _pathMaterial is not null
		&& _hoverMaterial is not null
		&& _targetMaterial is not null;

	private StandardMaterial3D EndpointMaterialForAp(int ap) =>
		ap switch
		{
			<= 2 => _endpointApLow!,
			3 => _endpointApMid!,
			_ => _endpointApHigh!,
		};

	private static float EndpointScaleForAp(int ap) =>
		ap switch
		{
			<= 2 => EndpointScaleLowAp,
			3 => EndpointScaleMidAp,
			_ => EndpointScaleHighAp,
		};

	private void SetCellHighlight(Coord coord, StandardMaterial3D material, float scale = HighlightCellScale)
	{
		if (_activeHighlights.TryGetValue(coord, out var existing))
		{
			existing.MaterialOverride = material;
			ApplyHighlightScale(existing, scale);
			return;
		}

		var cell = AcquireHighlightMesh(scale);
		cell.Position = WorldMapping.ToWorld(coord);
		cell.MaterialOverride = material;
		_activeHighlights[coord] = cell;
	}

	private MeshInstance3D AcquireHighlightMesh(float scale)
	{
		if (_freeHighlights.Count > 0)
		{
			var mesh = _freeHighlights.Dequeue();
			ApplyHighlightScale(mesh, scale);
			mesh.Visible = true;
			return mesh;
		}

		var cell = new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = HighlightSize(scale) },
			Visible = true,
		};
		PresentationLayers.MarkUx(cell);
		AddChild(cell);
		return cell;
	}

	private static void ApplyHighlightScale(MeshInstance3D mesh, float scale)
	{
		if (mesh.Mesh is BoxMesh box)
			box.Size = HighlightSize(scale);
	}

	private static Vector3 HighlightSize(float scale) =>
		Vector3.One * WorldMapping.CellSize * scale;

	private static StandardMaterial3D CreateMaterial(Color color) =>
		new()
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			AlbedoColor = color,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			// Avoid peer highlight cubes depth-occluding each other.
			DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
		};

	private void ReleaseActiveHighlights()
	{
		foreach (var mesh in _activeHighlights.Values)
		{
			mesh.Visible = false;
			_freeHighlights.Enqueue(mesh);
		}

		_activeHighlights.Clear();
	}
}
