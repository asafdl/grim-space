using GrimSpace.World.StarSystem.Pathfinding;

namespace GrimSpace.World.StarSystem;

public abstract record CourseCommandResult
{
	public sealed record Ignored : CourseCommandResult;

	public sealed record Queued(TransitPath Path) : CourseCommandResult;

	public sealed record Unreachable : CourseCommandResult;
}
