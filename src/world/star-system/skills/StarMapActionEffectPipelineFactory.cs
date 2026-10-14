using GrimSpace.Core.Actions;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Skills;

public static class StarMapActionEffectPipelineFactory
{
	public static ActionEffectPipeline<StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Build(
		IReadOnlyList<PilotSkillGrant> strategicGrants)
	{
		ArgumentNullException.ThrowIfNull(strategicGrants);
		var modifiers = new List<IActionEffectModifier<StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>>();
		var iKnowABot = strategicGrants.Count(grant => grant.SkillId == EPilotSkillId.IKnowABot);
		var drones = strategicGrants.Count(grant => grant.SkillId == EPilotSkillId.DronesYearnForMines);
		if (iKnowABot > 0)
			modifiers.Add(new IKnowABotModifier(iKnowABot));
		if (drones > 0)
			modifiers.Add(new DronesYearnForMinesModifier(drones));

		return modifiers.Count == 0
			? ActionEffectPipeline<StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>.Empty
			: new ActionEffectPipeline<StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>(modifiers);
	}

	public static void InstallPlayerPipeline(
		StarSystemOrchestrator orchestrator,
		RunPilotRegistry registry)
	{
		ArgumentNullException.ThrowIfNull(orchestrator);
		ArgumentNullException.ThrowIfNull(registry);
		if (orchestrator.PlayerId is null)
			return;

		var grants = registry.QueryLearnedForActive(RunPilotRegistry.IsStrategicPassiveTag);
		orchestrator.RuntimeFor(orchestrator.PlayerId).ReplaceActionEffects(Build(grants));
	}
}
