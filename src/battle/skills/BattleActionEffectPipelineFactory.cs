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
		if (!skills.Contains(EPilotSkillId.IonizedPayload))
			return ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>.Empty;

		return new ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>(
			[IonizedPayloadModifier.Instance]);
	}
}
