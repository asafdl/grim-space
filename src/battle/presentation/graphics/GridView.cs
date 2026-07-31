using System.Collections.Generic;
using Godot;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class GridView : Node3D
{
	private BoundedGrid? _grid;
	private readonly Dictionary<Coord, MeshInstance3D> _activeHighlights = new();
	private readonly Queue<MeshInstance3D> _freeHighlights = new();

	private StandardMaterial3D? _defaultMaterial;
	private StandardMaterial3D? _endpoint3Ap;
	private StandardMaterial3D? _endpoint4Ap;
	private StandardMaterial3D? _pathMaterial;
	private StandardMaterial3D? _hoverMaterial;
	private StandardMaterial3D? _hazardMaterial;
	private StandardMaterial3D? _targetMaterial;
	private StandardMaterial3D? _railgunMaterial;
	private StandardMaterial3D? _flakPortMaterial;
	private StandardMaterial3D? _flakStarboardMaterial;
	private StandardMaterial3D? _flakPreviewMaterial;

	public void Build(BoundedGrid grid)
	{
		_grid = grid;
		ReleaseActiveHighlights();

		_defaultMaterial = CreateMaterial(new Color(0.35f, 0.65f, 0.95f, 0.42f));
		_endpoint3Ap = _defaultMaterial;
		_endpoint4Ap = CreateMaterial(new Color(0.12f, 0.28f, 0.62f, 0.58f));
		_pathMaterial = CreateMaterial(new Color(0.45f, 0.5f, 0.6f, 0.22f));
		_hoverMaterial = CreateMaterial(new Color(0.95f, 0.95f, 1f, 0.65f));
		_hazardMaterial = CreateMaterial(new Color(0.95f, 0.25f, 0.15f, 0.55f));
		_targetMaterial = CreateMaterial(new Color(0.95f, 0.85f, 0.2f, 0.55f));
		_railgunMaterial = CreateMaterial(new Color(0.85f, 0.35f, 1f, 0.65f));
		_flakPortMaterial = CreateMaterial(new Color(0.9f, 0.55f, 0.15f, 0.5f));
		_flakStarboardMaterial = CreateMaterial(new Color(0.95f, 0.75f, 0.2f, 0.5f));
		_flakPreviewMaterial = CreateMaterial(new Color(1f, 0.85f, 0.25f, 0.7f));
	}

	public void ApplyFrame(PresentationFrame frame)
	{
		if (frame.ActiveUnit is null || frame.ShowOutcomeOverlay)
		{
			ReleaseActiveHighlights();
			return;
		}

		switch (frame.Mode)
		{
			case EPlayerMode.Move:
				SetMoveHighlights(
					frame.MoveOptions,
					frame.MovePath,
					frame.MoveTarget,
					frame.PreviewHazardCells);
				break;

			case EPlayerMode.Flak:
				SetFlakHighlights(
					frame.PreviewHazardCells,
					frame.ValidFlakPortCells,
					frame.ValidFlakStarboardCells,
					frame.FlakPreviewCells);
				break;

			case EPlayerMode.Railgun:
				SetRailgunHighlights(
					frame.RailgunCells,
					frame.RailgunPreviewCells,
					frame.PreviewHazardCells);
				break;
		}
	}

	public void SetMoveHighlights(
		IReadOnlyList<Option> options,
		IReadOnlyList<Coord> path,
		Coord? target,
		IReadOnlySet<Coord>? hazardCells = null)
	{
		if (!EnsureMaterials())
			return;

		ReleaseActiveHighlights();

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

	public void SetRailgunHighlights(
		IReadOnlySet<Coord> burstCells,
		IReadOnlySet<Coord> previewCells,
		IReadOnlySet<Coord>? hazardCells = null)
	{
		if (!EnsureMaterials())
			return;

		ReleaseActiveHighlights();

		if (hazardCells is not null)
		{
			foreach (var coord in hazardCells)
				SetCellMaterial(coord, _hazardMaterial!);
		}

		foreach (var coord in burstCells)
		{
			if (previewCells.Contains(coord))
				continue;

			SetCellMaterial(coord, _railgunMaterial!);
		}

		foreach (var coord in previewCells)
			SetCellMaterial(coord, _hoverMaterial!);
	}

	public void SetFlakHighlights(
		IReadOnlySet<Coord> hazardCells,
		IReadOnlySet<Coord> portCells,
		IReadOnlySet<Coord> starboardCells,
		IReadOnlySet<Coord> previewCells)
	{
		if (!EnsureMaterials())
			return;

		ReleaseActiveHighlights();

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
		&& _flakPortMaterial is not null
		&& _flakStarboardMaterial is not null
		&& _flakPreviewMaterial is not null;

	private void SetCellMaterial(Coord coord, StandardMaterial3D material)
	{
		if (_activeHighlights.TryGetValue(coord, out var existing))
		{
			existing.MaterialOverride = material;
			return;
		}

		var cell = AcquireHighlightMesh();
		cell.Position = WorldMapping.ToWorld(coord);
		cell.MaterialOverride = material;
		_activeHighlights[coord] = cell;
	}

	private MeshInstance3D AcquireHighlightMesh()
	{
		if (_freeHighlights.Count > 0)
		{
			var mesh = _freeHighlights.Dequeue();
			mesh.Visible = true;
			return mesh;
		}

		var cell = new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = Vector3.One * WorldMapping.CellSize * 0.92f },
			Visible = true,
		};
		AddChild(cell);
		return cell;
	}

	private static StandardMaterial3D CreateMaterial(Color color) =>
		new()
		{
			AlbedoColor = color,
			Roughness = 0.9f,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
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
