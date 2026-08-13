using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Abilities;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public sealed class HazardCellEntryEffect(Coord cell) : IEffect<BattleWorld, ActorRuntime>
{
	private UnitCombatSnapshot _snapshot;
	private bool _applied;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_snapshot = UnitCombatSnapshot.Capture(actor);
		_applied = false;

		var records = new List<IRecord>();
		foreach (var hazard in world.Hazards)
		{
			if (!hazard.Cells.Contains(cell))
				continue;

			_applied = true;
			if (HazardResolution.ApplyToUnitAt(hazard, actor, cell) is { } impact)
				records.Add(new Record<ImpactFacts>(impact));
		}

		return records;
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (!_applied)
			return;

		_snapshot.Restore(world.StateOf(actorId));
	}
}

public static class HazardResolution
{
	public static Coord ResolveCenter(EHazardKind kind, Coord shooterPosition, HashSet<Coord> cells) =>
		kind switch
		{
			EHazardKind.FlakBurst => shooterPosition,
			EHazardKind.RailgunBurst => shooterPosition,
			EHazardKind.TorpedoBlast => shooterPosition,
			_ => cells.Count > 0 ? cells.First() : Coord.Zero,
		};

	public static Hazard BuildTransient(
		EHazardKind kind,
		HashSet<Coord> cells,
		int damage,
		int momentumLoss,
		Coord center,
		string actorId = "") =>
		new()
		{
			Id = string.Empty,
			ActorId = actorId,
			Center = center,
			Frame = BodyFrame.WorldAligned(center),
			Cells = cells,
			Passable = true,
			Damage = damage,
			MomentumLoss = momentumLoss,
			Kind = kind,
		};

	public static IReadOnlyList<ImpactFacts> ApplyToUnitsInCells(Hazard hazard, IEnumerable<State> units)
	{
		var impacts = new List<ImpactFacts>();
		foreach (var unit in units)
		{
			if (!unit.IsAlive || !hazard.Cells.Contains(unit.Position))
				continue;

			if (ApplyToUnitAt(hazard, unit) is { } impact)
				impacts.Add(impact);
		}

		return impacts;
	}

	public static IReadOnlyList<ImpactFacts> ApplyScheduledResolve(
		EHazardKind kind,
		HashSet<Coord> cells,
		int damage,
		int momentumLoss,
		Coord shooterPosition,
		string actorId,
		IEnumerable<State> units)
	{
		var center = ResolveCenter(kind, shooterPosition, cells);
		var hazard = BuildTransient(kind, cells, damage, momentumLoss, center, actorId);
		return ApplyToUnitsInCells(hazard, units);
	}

	public static ImpactFacts? ApplyToUnitAt(Hazard hazard, State unit, Coord? attackOrigin = null)
	{
		var origin = attackOrigin ?? hazard.Center;
		var face = BodyFrame.From(unit).HitFaceFrom(origin);
		var shieldBefore = unit.ShieldPoints[face];
		var hullBefore = unit.HullPoints;
		var momBefore = unit.MomentumLevel;

		switch (hazard.Kind)
		{
			case EHazardKind.MissileZone:
				ApplyDirectedDamage(hazard, unit, face);
				unit.MomentumLevel = System.Math.Max(unit.MomentumLevel - hazard.MomentumLoss, 0);
				break;
			case EHazardKind.FlakBurst:
				ApplyDirectedDamage(hazard, unit, face);
				unit.MomentumLevel = System.Math.Max(unit.MomentumLevel - hazard.MomentumLoss, 0);
				if (unit.MomentumLevel < CombatConfig.FlakApPenaltyThreshold)
					unit.ApPenaltyNextTurn = true;
				break;
			case EHazardKind.RailgunBurst:
				ApplyDirectedDamage(hazard, unit, face);
				unit.MomentumLevel = System.Math.Max(unit.MomentumLevel - hazard.MomentumLoss, 0);
				break;
			case EHazardKind.TorpedoBlast:
				ApplyDirectedDamage(hazard, unit, face);
				break;
			default:
				return null;
		}

		var shieldDamage = shieldBefore - unit.ShieldPoints[face];
		var hullDamage = hullBefore - unit.HullPoints;
		var momLoss = momBefore - unit.MomentumLevel;
		if (shieldDamage == 0 && hullDamage == 0 && momLoss == 0)
			return null;

		return new ImpactFacts(
			SourceId: hazard.ActorId,
			TargetId: unit.Id,
			Cause: hazard.Kind,
			Face: face,
			ShieldDamage: shieldDamage,
			HullDamage: hullDamage,
			MomentumLoss: momLoss);
	}

	private static void ApplyDirectedDamage(Hazard hazard, State unit, ESpatialOrientation face)
	{
		if (hazard.Damage <= 0)
			return;

		Defense.ApplyDamage(unit, hazard.Damage, face);
	}
}
