using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Skills;

public sealed class IonizedPayloadModifier
	: IActionEffectModifier<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public static IonizedPayloadModifier Instance { get; } = new();

	public IEffect<BattleWorld, ActorRuntime> Modify(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime,
		IEffect<BattleWorld, ActorRuntime> effect)
	{
		if (!action.Tags.Contains(LightningCannonAction.TagNames.Attack)
			|| !action.Tags.Contains(LightningCannonAction.TagNames.Lightning))
			return effect;
		if (effect is not ApplyAreaDamageEffect area)
			return effect;

		return area.WithAdditionalShieldDamage(LightningCannonDef.IonizedPayloadShieldDamageBonus);
	}
}
