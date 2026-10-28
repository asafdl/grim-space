using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.PilotCapabilities;

public sealed class AdjacentDamageBonusModifier
	: IActionEffectModifier<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public static AdjacentDamageBonusModifier Instance { get; } = new();

	public IEffect<BattleWorld, ActorRuntime> Modify(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime,
		IEffect<BattleWorld, ActorRuntime> effect) =>
		effect is ApplyAreaDamageEffect area
			? area.WithAdjacentHitDamagePercent(
				PilotSkillPercents.DodgeThis.BonusHitDamagePercent,
				PilotSkillPercents.DodgeThis.BonusRangeCells)
			: effect;
}
