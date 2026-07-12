using Godot;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Presentation;

public static class WorldMapping
{
	public const float CellSize = 2f;

	public static Vector3 ToWorld(Coord coord) =>
		new(
			(coord.X + 0.5f) * CellSize,
			(coord.Y + 0.5f) * CellSize,
			(coord.Z + 0.5f) * CellSize);

	public static Vector3 GridCenter(BoundedGrid grid) =>
		new(
			grid.Width * CellSize * 0.5f,
			grid.Height * CellSize * 0.5f,
			grid.Depth * CellSize * 0.5f);
}
