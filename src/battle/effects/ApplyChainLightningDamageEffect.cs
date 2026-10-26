using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Effects;

public sealed class ApplyChainLightningDamageEffect(
	EImpactCause cause,
	IReadOnlySet<Coord> beamCells,
	IReadOnlyList<ChainLightningHop> hops,
	int baseDamage,
	int chainFalloffPercentPerHop,
	Coord origin,
	int shieldDamageBonus = 0,
	int shieldDamageBonusPercent = 0,
	int damageBonusPercent = 0) : IEffect<BattleWorld, ActorRuntime>
{
	public EImpactCause Cause => cause;
	public IReadOnlySet<Coord> BeamCells => beamCells;
	public IReadOnlyList<ChainLightningHop> Hops => hops;
	public int BaseDamage => baseDamage;
	public int ChainFalloffPercentPerHop => chainFalloffPercentPerHop;
	public Coord Origin => origin;
	public int ShieldDamageBonus => shieldDamageBonus;
	public int ShieldDamageBonusPercent => shieldDamageBonusPercent;
	public int DamageBonusPercent => damageBonusPercent;

	public ApplyChainLightningDamageEffect WithAdditionalShieldDamage(int amount) =>
		new(
			cause,
			beamCells,
			hops,
			baseDamage,
			chainFalloffPercentPerHop,
			origin,
			shieldDamageBonus + amount,
			shieldDamageBonusPercent,
			damageBonusPercent);

	public ApplyChainLightningDamageEffect WithShieldDamageBonusPercent(int percent) =>
		new(
			cause,
			beamCells,
			hops,
			baseDamage,
			chainFalloffPercentPerHop,
			origin,
			shieldDamageBonus,
			percent,
			damageBonusPercent);

	public ApplyChainLightningDamageEffect WithDamageBonusPercent(int percent) =>
		new(
			cause,
			beamCells,
			hops,
			baseDamage,
			chainFalloffPercentPerHop,
			origin,
			shieldDamageBonus,
			shieldDamageBonusPercent,
			percent);

	private Dictionary<string, UnitCombatSnapshot> _snapshots = [];

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		_snapshots = new Dictionary<string, UnitCombatSnapshot>();
		var source = UnitRegistry.For(world).UnitOf(actorId);
		var records = new List<IRecord>
		{
			new Record<AreaDamageFacts>(new AreaDamageFacts(
				actorId,
				cause,
				origin,
				beamCells,
				hops.Select(hop => hop.Position).ToArray())),
		};

		for (var hopIndex = 0; hopIndex < hops.Count; hopIndex++)
		{
			var hop = hops[hopIndex];
			var unit = world.StateOf(hop.TargetId);
			if (!unit.IsAlive)
				continue;

			_snapshots[unit.Id] = UnitCombatSnapshot.Capture(unit);
			var damage = ChainLightning.DamageAtHop(
				baseDamage,
				hopIndex,
				chainFalloffPercentPerHop);
			var isAlliedSource = source.RelationTo(world.UnitRegistry.UnitOf(unit.Id)) == EUnitRelation.Ally;
			if (ApplyTo(unit, actorId, damage, isAlliedSource) is { } impact)
				records.Add(new Record<ImpactFacts>(impact));
		}

		return records;
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var (unitId, snapshot) in _snapshots)
			snapshot.Restore(world.StateOf(unitId));
	}

	private ImpactFacts? ApplyTo(
		State unit,
		string actorId,
		int damage,
		bool isAlliedSource)
	{
		var face = BodyFrame.From(unit).HitFaceFrom(origin);
		var shieldBefore = Defense.EffectiveShield(unit, face);
		var hullBefore = unit.HullPoints;
		var appliedDamage = damage;
		if (damageBonusPercent != 0)
			appliedDamage = ScalePercent(appliedDamage, 100 + damageBonusPercent);
		var appliedShieldDamageBonus = shieldDamageBonus;
		if (shieldDamageBonusPercent > 0)
			appliedShieldDamageBonus += PortionOf(appliedDamage, shieldDamageBonusPercent);

		if (isAlliedSource && unit.InTheFrayActive)
		{
			var retained = PilotSkillPercents.InTheFray.AlliedDamageTakenPercent;
			var reducedShieldDamage = ScalePercent(appliedDamage + appliedShieldDamageBonus, retained);
			appliedDamage = ScalePercent(appliedDamage, retained);
			appliedShieldDamageBonus = reducedShieldDamage - appliedDamage;
		}

		if (damage > 0
			&& unit.DefensiveManeuversMpSpentLastTurn == 0
			&& RollsDeflection(unit))
		{
			var retained = PilotSkillPercents.DefensiveManeuvers.DeflectDamageTakenPercent;
			appliedDamage = ScalePercent(appliedDamage, retained);
			appliedShieldDamageBonus = ScalePercent(appliedShieldDamageBonus, retained);
		}

		if (appliedDamage > 0)
			Defense.ApplyDamage(unit, appliedDamage, face, appliedShieldDamageBonus);

		var shieldAfter = Defense.EffectiveShield(unit, face);
		var shieldDamage = shieldBefore - shieldAfter;
		var hullDamage = hullBefore - unit.HullPoints;
		if (shieldDamage == 0 && hullDamage == 0)
			return null;

		return new ImpactFacts(
			SourceId: actorId,
			TargetId: unit.Id,
			Cause: cause,
			Face: face,
			ShieldDamage: shieldDamage,
			HullDamage: hullDamage);
	}

	private static int ScalePercent(int amount, int percent) =>
		(int)System.Math.Round(amount * percent / 100m, MidpointRounding.AwayFromZero);

	private static int PortionOf(int amount, int percent) => ScalePercent(amount, percent);

	private bool RollsDeflection(State unit)
	{
		var rollIndex = unit.DefensiveManeuversRollIndex++;
		var random = new StableRandom(
			new StableSeedMixer()
				.Add(unit.Id)
				.Add(rollIndex)
				.Add((int)cause)
				.Value);
		return random.NextDouble()
			< PilotSkillPercents.DefensiveManeuvers.DeflectChancePercent / 100.0;
	}
}
