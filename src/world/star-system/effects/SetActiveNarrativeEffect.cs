using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class SetActiveNarrativeEffect : IEffect<StarMap, Runtime.ActorRuntime>
{
	private readonly string? _narrativeId;

	public SetActiveNarrativeEffect(string? narrativeId) => _narrativeId = narrativeId;

	public IReadOnlyList<IRecord> Apply(StarMap world, Runtime.ActorRuntime runtime, string actorId)
	{
		world.ActiveNarrativeId = _narrativeId;
		return [];
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
