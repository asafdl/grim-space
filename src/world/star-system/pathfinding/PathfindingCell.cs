namespace GrimSpace.World.StarSystem.Pathfinding;

public readonly record struct PathfindingCell(
	bool Blocked,
	double WeightScale,
	double SpeedMultiplier)
{
	public static PathfindingCell OpenSpace => new(false, 1.5, 1.0);

	public static PathfindingCell RouteCorridor => new(false, 1.0, 1.5);

	public static PathfindingCell Obstacle => new(true, 1.0, 1.0);
}
