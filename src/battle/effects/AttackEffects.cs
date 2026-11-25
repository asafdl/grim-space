using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public static class AttackEffects
{
	public static IEnumerable<IEffect<BattleWorld, ActorRuntime>> Area(
		BattleWorld world,
		string actorId,
		EImpactCause cause,
		IReadOnlySet<Coord> cells,
		int damage,
		Coord origin,
		int hullBreachDamagePercent = 100,
		bool alliesTakeDamage = true)
	{
		yield return new AnnounceAreaDamageEffect(new AreaDamageFacts(actorId, cause, origin, cells));
		var targets = world.UnitsInCells(actorId, cells)
			.Where(entry => entry.Relation == EUnitRelation.Opponent
				|| (alliesTakeDamage && entry.Relation == EUnitRelation.Ally))
			.Select(entry => entry.Unit.State)
			.OrderBy(state => origin.ManhattanDistanceTo(state.Position))
			.ThenBy(state => state.Id, StringComparer.Ordinal)
			.ToArray();
		var weaponBonus = world.StateOf(actorId).Loadout.WeaponDamagePercent;
		foreach (var target in targets)
			yield return new DamageUnitEffect(cause, target.Id, origin, damage, hullBreachDamagePercent)
			{
				BonusPercent = weaponBonus,
			};
	}

	public static IEnumerable<IEffect<BattleWorld, ActorRuntime>> Chain(
		BattleWorld world,
		string actorId,
		EImpactCause cause,
		ChainLightningResult chain,
		int baseDamage,
		int falloffPercentPerHop,
		Coord origin)
	{
		yield return new AnnounceAreaDamageEffect(new AreaDamageFacts(
			actorId,
			cause,
			origin,
			chain.BeamCells,
			chain.Hops.Select(hop => hop.Position).ToArray()));
		var weaponBonus = world.StateOf(actorId).Loadout.WeaponDamagePercent;
		for (var hopIndex = 0; hopIndex < chain.Hops.Count; hopIndex++)
			yield return new DamageUnitEffect(
				cause,
				chain.Hops[hopIndex].TargetId,
				origin,
				ChainLightning.DamageAtHop(baseDamage, hopIndex, falloffPercentPerHop))
			{
				BonusPercent = weaponBonus,
			};
	}

	public static IEnumerable<IEffect<BattleWorld, ActorRuntime>> Erosion(
		BattleWorld world,
		IEnumerable<Coord> hitCells,
		Coord origin)
	{
		var cells = hitCells.Where(cell => cell != origin).ToHashSet();
		if (cells.Any(cell => world.AsteroidBeltAt(cell) is not null))
			yield return new ErodeAsteroidBeltCoverEffect(cells);
	}
}
