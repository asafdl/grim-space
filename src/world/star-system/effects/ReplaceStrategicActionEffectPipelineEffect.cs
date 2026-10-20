using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Pilots;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.PilotCapabilities;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class ReplaceStrategicActionEffectPipelineEffect(
	IReadOnlyList<PilotCapabilityGrant> strategicGrants)
	: IEffect<StarMap, ActorRuntime>
{
	private ActionEffectPipeline<StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> _previous =
		ActionEffectPipeline<StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>.Empty;

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		_previous = runtime.ActionEffects;
		runtime.ReplaceActionEffects(StarMapPilotCapabilityPipelineFactory.Build(strategicGrants));
		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId) =>
		runtime.ReplaceActionEffects(_previous);
}
