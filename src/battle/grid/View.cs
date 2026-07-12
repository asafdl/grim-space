using System.Collections.Generic;
using Godot;
using GrimSpace.Battle.Movement;
using GrimSpace.Domain.Grid;
using BattleGrid = GrimSpace.Battle.Grid.Grid;

namespace GrimSpace.Battle.Grid;

public partial class View : Node3D
{
	public const float CellSize = 2f;

	private BattleGrid? _grid;
	private readonly Dictionary<Coord, MeshInstance3D> _highlights = new();

	private StandardMaterial3D? _defaultMaterial;
	private StandardMaterial3D? _endpoint3Ap;
	private StandardMaterial3D? _endpoint4Ap;
	private StandardMaterial3D? _pathMaterial;
	private StandardMaterial3D? _hoverMaterial;
	private StandardMaterial3D? _hazardMaterial;
	private StandardMaterial3D? _targetMaterial;
	private StandardMaterial3D? _railgunMaterial;
	private StandardMaterial3D? _aimMaterial;

	public void Build(BattleGrid grid)
	{
		_grid = grid;
		ClearHighlightMeshes();

		_defaultMaterial = CreateMaterial(new Color(0.35f, 0.65f, 0.95f, 0.42f));
		_endpoint3Ap = _defaultMaterial;
		_endpoint4Ap = CreateMaterial(new Color(0.12f, 0.28f, 0.62f, 0.58f));
		_pathMaterial = CreateMaterial(new Color(0.45f, 0.5f, 0.6f, 0.22f));
		_hoverMaterial = CreateMaterial(new Color(0.95f, 0.95f, 1f, 0.65f));
		_hazardMaterial = CreateMaterial(new Color(0.95f, 0.25f, 0.15f, 0.55f));
		_targetMaterial = CreateMaterial(new Color(0.95f, 0.85f, 0.2f, 0.55f));
		_railgunMaterial = CreateMaterial(new Color(0.85f, 0.35f, 1f, 0.65f));
		_aimMaterial = CreateMaterial(new Color(0.35f, 0.8f, 1f, 0.45f));
	}

	public void ClearHighlights()
	{
		ClearHighlightMeshes();
	}

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

	private bool EnsureMaterials() =>
		_grid is not null
		&& _defaultMaterial is not null
		&& _endpoint4Ap is not null
		&& _pathMaterial is not null
		&& _hoverMaterial is not null
		&& _hazardMaterial is not null
		&& _targetMaterial is not null
		&& _railgunMaterial is not null
		&& _aimMaterial is not null;

	public static Vector3 ToWorld(Coord coord) =>
		new(
			(coord.X + 0.5f) * CellSize,
			(coord.Y + 0.5f) * CellSize,
			(coord.Z + 0.5f) * CellSize);

	public static Vector3 GridCenter(BattleGrid grid) =>
		new(
			grid.Width * CellSize * 0.5f,
			grid.Height * CellSize * 0.5f,
			grid.Depth * CellSize * 0.5f);

	private void SetCellMaterial(Coord coord, StandardMaterial3D material)
	{
		if (_highlights.TryGetValue(coord, out var existing))
		{
			existing.MaterialOverride = material;
			return;
		}

		var cell = new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = Vector3.One * CellSize * 0.92f },
			Position = ToWorld(coord),
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
			child.QueueFree();

		_highlights.Clear();
	}
}
