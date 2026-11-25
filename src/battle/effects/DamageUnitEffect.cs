using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public readonly record struct HitStep(int RetainPercent, int AddedShieldDamagePercent = 0);

public sealed record DamageUnitEffect(
	EImpactCause Cause,
	string TargetId,
	Coord Origin,
	int Damage,
	int HullBreachDamagePercent = 100) : IEffect<BattleWorld, ActorRuntime>
{
	public int BonusPercent { get; init; }
	public IReadOnlyList<int> DamagePercents { get; init; } = [];
	public int ShieldDamageBonusPercent { get; init; }
	public IReadOnlyList<HitStep> HitSteps { get; init; } = [];
	public IReadOnlyList<int> HullDamagePercents { get; init; } = [];

	private UnitCombatSnapshot? _snapshot;

	public State Target(BattleWorld world) => world.StateOf(TargetId);

	public ESpatialOrientation Face(BattleWorld world) => BodyFrame.From(Target(world)).HitFaceFrom(Origin);

	public DamageUnitEffect AddBonusPercent(int percent) => this with { BonusPercent = BonusPercent + percent };

	public DamageUnitEffect ScaleDamage(int percent) => this with { DamagePercents = [.. DamagePercents, percent] };

	public DamageUnitEffect ReduceHit(int retainPercent, int times = 1) =>
		this with { HitSteps = [.. HitSteps, .. Enumerable.Repeat(new HitStep(retainPercent), times)] };

	public DamageUnitEffect AddShieldDamageOnHit(int percent) =>
		this with { HitSteps = [.. HitSteps, new HitStep(100, percent)] };

	public DamageUnitEffect ScaleHullDamage(int percent) =>
		this with { HullDamagePercents = [.. HullDamagePercents, percent] };

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var unit = Target(world);
		_snapshot = null;
		if (!unit.IsAlive)
			return [];

		_snapshot = UnitCombatSnapshot.Capture(unit);
		var face = Face(world);
		var shieldBefore = Defense.EffectiveShield(unit, face);
		var hullBefore = unit.CurrentHullPoints;

		var damage = Defense.ScalePercent(Damage, 100 + BonusPercent);
		foreach (var percent in DamagePercents)
			damage = Defense.ScalePercent(damage, percent);
		if (HullBreachDamagePercent != 100 && damage > shieldBefore)
			damage = Defense.ScalePercent(damage, HullBreachDamagePercent);

		var shieldBonus = Defense.ScalePercent(damage, ShieldDamageBonusPercent);
		foreach (var step in HitSteps)
		{
			if (step.AddedShieldDamagePercent != 0)
			{
				shieldBonus += Defense.ScalePercent(damage, step.AddedShieldDamagePercent);
				continue;
			}

			var total = Defense.ScalePercent(damage + shieldBonus, step.RetainPercent);
			damage = Defense.ScalePercent(damage, step.RetainPercent);
			shieldBonus = total - damage;
		}

		Defense.ApplyDamage(unit, damage, face, shieldBonus, HullDamagePercents);

		var shieldDamage = shieldBefore - Defense.EffectiveShield(unit, face);
		var hullDamage = hullBefore - unit.CurrentHullPoints;
		if (shieldDamage == 0 && hullDamage == 0)
			return [];

		return
		[
			new Record<ImpactFacts>(new ImpactFacts(
				SourceId: actorId,
				TargetId: TargetId,
				Cause: Cause,
				Face: face,
				ShieldDamage: shieldDamage,
				HullDamage: hullDamage,
				ShieldBefore: shieldBefore,
				HullBefore: hullBefore)),
		];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId) =>
		_snapshot?.Restore(Target(world));
}
