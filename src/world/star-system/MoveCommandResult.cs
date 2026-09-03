using GrimSpace.World.StarSystem.Pathfinding;

namespace GrimSpace.World.StarSystem;

public abstract record MoveCommandResult
{
	public sealed record Queued(TransitPath Path) : MoveCommandResult;

	public sealed record Unreachable : MoveCommandResult;
}
