using System.Collections.Generic;
using Godot;

namespace GrimSpace.Battle.Grid;

public partial class View : Node3D
{
	public const float CellSize = 2f;

	private readonly Dictionary<Coord, MeshInstance3D> _cells = new();
	private StandardMaterial3D? _defaultMaterial;
	private StandardMaterial3D? _highlightMaterial;
	private StandardMaterial3D? _pathMaterial;
	private StandardMaterial3D? _hoverMaterial;

	public void Build(Grid grid)
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

		_pathMaterial = new StandardMaterial3D
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

		var cellMesh = new BoxMesh { Size = Vector3.One * CellSize * 0.92f };

		for (var x = 0; x < grid.Width; x++)
		{
			for (var y = 0; y < grid.Height; y++)
			{
				for (var z = 0; z < grid.Depth; z++)
				{
					var coord = new Coord(x, y, z);
					var cell = new MeshInstance3D
					{
						Mesh = cellMesh,
						Position = ToWorld(coord),
						MaterialOverride = _defaultMaterial,
					};
					AddChild(cell);
					_cells[coord] = cell;
				}
			}
		}
	}

	public void SetHighlights(
		IEnumerable<Coord> endpoints,
		IReadOnlyList<Coord> path,
		Coord? target)
	{
		if (_defaultMaterial is null || _highlightMaterial is null || _pathMaterial is null || _hoverMaterial is null)
			return;

		var endpointSet = new HashSet<Coord>(endpoints);
		var pathSet = new HashSet<Coord>(path);

		foreach (var (coord, cell) in _cells)
		{
			if (target is not null && coord == target.Value)
				cell.MaterialOverride = _hoverMaterial;
			else if (endpointSet.Contains(coord))
				cell.MaterialOverride = _highlightMaterial;
			else if (pathSet.Contains(coord))
				cell.MaterialOverride = _pathMaterial;
			else
				cell.MaterialOverride = _defaultMaterial;
		}
	}

	public static Vector3 ToWorld(Coord coord) =>
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
