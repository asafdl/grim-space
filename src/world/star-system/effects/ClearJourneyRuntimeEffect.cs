using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class ClearJourneyRuntimeEffect : IEffect<StarMap, Runtime.ActorRuntime>
{
	public static ClearJourneyRuntimeEffect Instance { get; } = new();

	public IReadOnlyList<IRecord> Apply(StarMap world, Runtime.ActorRuntime runtime, string actorId)
	{
		runtime.CachedPath = null;
		runtime.ClearPendingCompletion();
		return [];
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
