using GrimSpace.Battle.Runtime;

namespace GrimSpace.Battle.Movement;

/// <summary>
/// Path-related runtime fields captured at a search node — avoids retaining full actor runtimes.
/// </summary>
public readonly record struct MovePathSnapshot(
	int PathApSpent,
	int MinPathApCost,
	bool SpinBraked,
	int PathForwardSteps,
	int UsedDirectionsMask)
{
	public bool IsMovePathStarted => PathForwardSteps > 0 || UsedDirectionsMask > 0;

	public static MovePathSnapshot From(ActorRuntime runtime) =>
		new(
			runtime.PathApSpent,
			runtime.MinPathApCost,
			runtime.SpinBraked,
			runtime.PathForwardSteps,
			runtime.UsedDirectionsMask);
}
