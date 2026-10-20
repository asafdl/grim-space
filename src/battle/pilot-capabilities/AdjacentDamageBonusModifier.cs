using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.PilotCapabilities;

public sealed class AdjacentDamageBonusModifier
	: IActionEffectModifier<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public const int AdjacentTargetDamageBonus = 2;

	public static AdjacentDamageBonusModifier Instance { get; } = new();

	public IEffect<BattleWorld, ActorRuntime> Modify(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime,
		IEffect<BattleWorld, ActorRuntime> effect) =>
		effect is ApplyAreaDamageEffect area
			? area.WithAdjacentTargetDamageBonus(AdjacentTargetDamageBonus)
			: effect;
}
