using System.Collections.Generic;
using Godot;
using GrimSpace.Battle;

namespace GrimSpace.Battle;

public partial class GridView : Node3D
{
	public const float CellSize = 2f;

	private readonly Dictionary<GridCoord, MeshInstance3D> _cells = new();
	private StandardMaterial3D? _defaultMaterial;
	private StandardMaterial3D? _highlightMaterial;
	private StandardMaterial3D? _aimPathMaterial;
	private StandardMaterial3D? _hoverMaterial;

	public void Build(BattleGrid grid)
	{
		ClearCells();

		_defaultMaterial = new StandardMaterial3D
		{
			AlbedoColor = new Color(0.15f, 0.18f, 0.25f, 0.12f),
			Roughness = 0.9f,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
		};

		_highlightMaterial = new StandardMaterial3D
		{
			AlbedoColor = new Color(0.25f, 0.55f, 0.85f, 0.5f),
			Roughness = 0.9f,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
		};

		_aimPathMaterial = new StandardMaterial3D
		{
			AlbedoColor = new Color(0.45f, 0.5f, 0.6f, 0.22f),
			Roughness = 0.9f,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
		};

		_hoverMaterial = new StandardMaterial3D
		{
			AlbedoColor = new Color(0.95f, 0.95f, 1f, 0.65f),
			Roughness = 0.9f,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
		};

		var cellMesh = new BoxMesh
		{
			Size = Vector3.One * CellSize * 0.92f,
		};

		for (var x = 0; x < grid.Width; x++)
		{
			for (var y = 0; y < grid.Height; y++)
			{
				for (var z = 0; z < grid.Depth; z++)
				{
					var coord = new GridCoord(x, y, z);
					var cell = new MeshInstance3D
					{
						Mesh = cellMesh,
						Position = GridToWorld(coord),
						MaterialOverride = _defaultMaterial,
					};
					AddChild(cell);
					_cells[coord] = cell;
				}
			}
		}
	}

	public void SetCellHighlights(
		IEnumerable<GridCoord> reachable,
		IReadOnlyList<GridCoord> aimPath,
		GridCoord? aimTarget)
	{
		if (_defaultMaterial is null || _highlightMaterial is null || _aimPathMaterial is null || _hoverMaterial is null)
			return;

		var reachableSet = new HashSet<GridCoord>(reachable);
		var aimPathSet = new HashSet<GridCoord>(aimPath);

		foreach (var (coord, cell) in _cells)
		{
			if (aimTarget is not null && coord == aimTarget.Value)
				cell.MaterialOverride = _hoverMaterial;
			else if (reachableSet.Contains(coord))
				cell.MaterialOverride = _highlightMaterial;
			else if (aimPathSet.Contains(coord))
				cell.MaterialOverride = _aimPathMaterial;
			else
				cell.MaterialOverride = _defaultMaterial;
		}
	}

	public static Vector3 GridToWorld(GridCoord coord) =>
		new(
			(coord.X + 0.5f) * CellSize,
			(coord.Y + 0.5f) * CellSize,
			(coord.Z + 0.5f) * CellSize);

	private void ClearCells()
	{
		foreach (var child in GetChildren())
			child.QueueFree();

		_cells.Clear();
	}
}
