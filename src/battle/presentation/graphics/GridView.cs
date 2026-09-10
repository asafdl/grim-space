using System.Collections.Generic;
using Godot;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class GridView : Node3D
{
	private const float DotRadius = 0.18f;
	private const float PathDotScale = 0.7f;
	private const float EndpointDotScale = 1f;
	private const float HoverDotScale = 1.4f;

	private BoundedGrid? _grid;
	private readonly Dictionary<Coord, MeshInstance3D> _activeHighlights = new();
	private readonly Queue<MeshInstance3D> _freeHighlights = new();

	private StandardMaterial3D? _endpointMaterial;
	private StandardMaterial3D? _pathMaterial;
	private StandardMaterial3D? _hoverMaterial;

	public void Build(BoundedGrid grid)
	{
		_grid = grid;
		ReleaseActiveHighlights();

		_endpointMaterial = CreateMaterial(new Color(0.32f, 0.48f, 0.78f, 0.7f));
		_pathMaterial = CreateMaterial(new Color(0.45f, 0.5f, 0.6f, 0.45f));
		_hoverMaterial = CreateMaterial(new Color(0.95f, 0.95f, 1f, 0.65f));
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
				SetMoveHighlights(frame.MovePaths, frame.MovePath, frame.MoveTarget);
				break;

			case EPlayerMode.Flak:
			case EPlayerMode.Railgun:
			case EPlayerMode.Torpedo:
			case EPlayerMode.Detonate:
				// Weapon volumes are drawn by *PreviewView; keep cells for picking only.
				ReleaseActiveHighlights();
				break;
		}
	}

	public void SetMoveHighlights(
		IReadOnlyList<MovePathOption> paths,
		IReadOnlyList<Coord> path,
		Coord? target)
	{
		if (!EnsureMaterials())
			return;

		ReleaseActiveHighlights();

		var endpoints = new HashSet<Coord>();
		foreach (var option in paths)
			endpoints.Add(option.EndPosition);

		var pathSet = new HashSet<Coord>(path);

		PresentationDiagnostics.LogMovePreviewHighlights(paths.Count, endpoints.Count);

		foreach (var coord in pathSet)
		{
			if (coord == target)
				continue;

			SetDot(coord, _pathMaterial!, PathDotScale);
		}

		foreach (var coord in endpoints)
		{
			if (coord == target || pathSet.Contains(coord))
				continue;

			SetDot(coord, _endpointMaterial!, EndpointDotScale);
		}

		if (target is Coord hovered)
			SetDot(hovered, _hoverMaterial!, HoverDotScale);
	}

	private bool EnsureMaterials() =>
		_grid is not null
		&& _endpointMaterial is not null
		&& _pathMaterial is not null
		&& _hoverMaterial is not null;

	private void SetDot(Coord coord, StandardMaterial3D material, float scale)
	{
		if (_activeHighlights.TryGetValue(coord, out var existing))
		{
			existing.MaterialOverride = material;
			ApplyDotScale(existing, scale);
			return;
		}

		var dot = AcquireDot(scale);
		dot.Position = WorldMapping.ToWorld(coord);
		dot.MaterialOverride = material;
		_activeHighlights[coord] = dot;
	}

	private MeshInstance3D AcquireDot(float scale)
	{
		if (_freeHighlights.Count > 0)
		{
			var mesh = _freeHighlights.Dequeue();
			ApplyDotScale(mesh, scale);
			mesh.Visible = true;
			return mesh;
		}

		var dot = new MeshInstance3D
		{
			Mesh = CreateDotMesh(scale),
			Visible = true,
		};
		PresentationLayers.MarkUx(dot);
		AddChild(dot);
		return dot;
	}

	private static void ApplyDotScale(MeshInstance3D mesh, float scale)
	{
		if (mesh.Mesh is not SphereMesh sphere)
			return;

		sphere.Radius = DotRadius * scale;
		sphere.Height = DotRadius * scale * 2f;
	}

	private static SphereMesh CreateDotMesh(float scale) =>
		new()
		{
			Radius = DotRadius * scale,
			Height = DotRadius * scale * 2f,
			RadialSegments = 12,
			Rings = 6,
		};

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
