using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.PilotCapabilities;

public sealed class LightningShieldDamageBonusModifier
	: IActionEffectModifier<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public static LightningShieldDamageBonusModifier Instance { get; } = new();

	public IEffect<BattleWorld, ActorRuntime> Modify(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime,
		IEffect<BattleWorld, ActorRuntime> effect)
		=> effect switch
		{
			ApplyAreaDamageEffect area => area.WithShieldDamageBonusPercent(
				PilotSkillPercents.IonizedPayload.ShieldDamageBonusPercent),
			ApplyChainLightningDamageEffect chain => chain.WithShieldDamageBonusPercent(
				PilotSkillPercents.IonizedPayload.ShieldDamageBonusPercent),
			_ => effect,
		};
}
