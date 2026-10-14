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

	public static IReadOnlyList<PilotSkillGrant> StrategicGrantsFrom(
		RunPilotRegistry registry,
		IReadOnlyList<string> partyShipIds) =>
		registry.QueryLearnedForParty(RunPilotRegistry.IsStrategicPassiveTag, partyShipIds);

	public static void InstallPlayerPipeline(
		StarSystemOrchestrator orchestrator,
		RunPilotRegistry registry,
		IReadOnlyList<string> partyShipIds)
	{
		ArgumentNullException.ThrowIfNull(orchestrator);
		ArgumentNullException.ThrowIfNull(registry);
		ArgumentNullException.ThrowIfNull(partyShipIds);
		if (orchestrator.PlayerId is null)
			return;

		var grants = StrategicGrantsFrom(registry, partyShipIds);
		orchestrator.RuntimeFor(orchestrator.PlayerId).ReplaceActionEffects(Build(grants));
	}

	public static IReadOnlyList<PilotSkillGrant> GrantsFromPipeline(
		ActionEffectPipeline<StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> pipeline)
	{
		ArgumentNullException.ThrowIfNull(pipeline);
		var grants = new List<PilotSkillGrant>();
		foreach (var modifier in pipeline.Modifiers)
		{
			switch (modifier)
			{
				case IKnowABotModifier bot:
					AppendStacks(grants, EPilotSkillId.IKnowABot, bot.StackCount);
					break;
				case DronesYearnForMinesModifier drones:
					AppendStacks(grants, EPilotSkillId.DronesYearnForMines, drones.StackCount);
					break;
			}
		}

		return grants;
	}

	private static void AppendStacks(
		List<PilotSkillGrant> grants,
		EPilotSkillId skillId,
		int stackCount)
	{
		for (var index = 0; index < stackCount; index++)
			grants.Add(new PilotSkillGrant(skillId, string.Empty));
	}
}
