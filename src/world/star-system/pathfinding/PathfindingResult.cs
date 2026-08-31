namespace GrimSpace.World.StarSystem.Pathfinding;

public abstract record PathfindingResult
{
	public sealed record Found(TransitPath Path) : PathfindingResult;

	public sealed record Unreachable : PathfindingResult;
}
