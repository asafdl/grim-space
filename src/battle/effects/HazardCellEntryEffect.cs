using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

// TODO: Remove — hazard damage is intended only on ResolveHazardAction, not on cell entry during movement.
public sealed class HazardCellEntryEffect(Coord cell) : IEffect<BattleWorld, ActorRuntime>
{
	private UnitCombatSnapshot _snapshot;
	private bool _applied;

	public void Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_snapshot = UnitCombatSnapshot.Capture(actor);
		_applied = false;

		foreach (var hazard in world.Hazards)
		{
			if (!hazard.Cells.Contains(cell))
				continue;

			_applied = true;
			HazardResolution.ApplyToUnitAt(hazard, actor, cell);
		}
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

	public static void ApplyToUnitsInCells(Hazard hazard, IEnumerable<State> units)
	{
		foreach (var unit in units)
		{
			if (!unit.IsAlive || !hazard.Cells.Contains(unit.Position))
				continue;

			ApplyToUnitAt(hazard, unit);
		}
	}

	public static void ApplyScheduledResolve(
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
		ApplyToUnitsInCells(hazard, units);
	}

	public static void ApplyToUnitAt(Hazard hazard, State unit, Coord? attackOrigin = null)
	{
		switch (hazard.Kind)
		{
			case EHazardKind.MissileZone:
				ApplyDirectedDamage(hazard, unit, attackOrigin);
				unit.MomentumLevel = System.Math.Max(unit.MomentumLevel - hazard.MomentumLoss, 0);
				break;
			case EHazardKind.FlakBurst:
				ApplyDirectedDamage(hazard, unit, attackOrigin);
				unit.MomentumLevel = System.Math.Max(unit.MomentumLevel - hazard.MomentumLoss, 0);
				if (unit.MomentumLevel < CombatConfig.FlakApPenaltyThreshold)
					unit.ApPenaltyNextTurn = true;
				break;
			case EHazardKind.RailgunBurst:
				ApplyDirectedDamage(hazard, unit, attackOrigin);
				unit.MomentumLevel = System.Math.Max(unit.MomentumLevel - hazard.MomentumLoss, 0);
				break;
		}
	}

	private static void ApplyDirectedDamage(Hazard hazard, State unit, Coord? attackOrigin)
	{
		if (hazard.Damage <= 0)
			return;

		var origin = attackOrigin ?? hazard.Center;
		var face = BodyFrame.From(unit).HitFaceFrom(origin);
		Defense.ApplyDamage(unit, hazard.Damage, face);
	}
}
