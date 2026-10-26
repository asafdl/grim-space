using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.PilotCapabilities;

public sealed class OverdriveDamageBonusModifier
	: IActionEffectModifier<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public const int BonusPercent = 80;

	public static OverdriveDamageBonusModifier Instance { get; } = new();

	public IEffect<BattleWorld, ActorRuntime> Modify(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime,
		IEffect<BattleWorld, ActorRuntime> effect)
	{
		if (!IsEligible(world, action.ActorId))
			return effect;

		return effect switch
		{
			ApplyAreaDamageEffect area => area.WithDamageBonusPercent(BonusPercent),
			ApplyChainLightningDamageEffect chain => chain.WithDamageBonusPercent(BonusPercent),
			_ => effect,
		};
	}

	internal static bool IsEligible(BattleWorld world, string actorId)
	{
		if (world is null || !world.UnitRegistry.Contains(actorId))
			return false;

		var actor = world.StateOf(actorId);
		if (!actor.HeatProfile.Participates)
			return false;
		if (actor.Overdrive)
			return true;

		var capacity = actor.HeatProfile.EffectiveCapacity;
		if (actor.Heat >= capacity)
			return true;

		return actor.Heat + actor.HeatProfile.ScaleGain(HeatChangeEffect.PerAbilityUse) >= capacity;
	}
}
