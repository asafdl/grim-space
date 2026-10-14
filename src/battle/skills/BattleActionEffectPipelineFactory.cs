using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Skills;

public static class BattleActionEffectPipelineFactory
{
	public static ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Build(
		IReadOnlySet<EPilotSkillId> skills)
	{
		ArgumentNullException.ThrowIfNull(skills);
		var modifiers = new List<IActionEffectModifier<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>>();
		if (skills.Contains(EPilotSkillId.DodgeThis))
			modifiers.Add(DodgeThisModifier.Instance);
		if (skills.Contains(EPilotSkillId.IonizedPayload))
			modifiers.Add(IonizedPayloadModifier.Instance);

		if (modifiers.Count == 0)
			return ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>.Empty;

		return new ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>(
			modifiers);
	}
}
