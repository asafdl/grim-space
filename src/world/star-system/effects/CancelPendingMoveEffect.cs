using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class CancelPendingMoveEffect : IEffect<StarMap, Runtime.ActorRuntime>
{
	public static CancelPendingMoveEffect Instance { get; } = new();

	public IReadOnlyList<IRecord> Apply(StarMap world, Runtime.ActorRuntime runtime, string actorId)
	{
		if (runtime.PendingCompletion is not { } action)
			return [];

		world.Timeline.CancelPending(runtime.PendingCompletionTick, action);
		runtime.ClearPendingCompletion();
		return [];
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
