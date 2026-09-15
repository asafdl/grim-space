using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Abilities;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

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
		Coord shooterPosition,
		string actorId,
		IEnumerable<State> units)
	{
		var center = ResolveCenter(kind, shooterPosition, cells);
		var hazard = BuildTransient(kind, cells, damage, center, actorId);
		return ApplyToUnitsInCells(hazard, units);
	}

	public static ImpactFacts? ApplyToUnitAt(Hazard hazard, State unit, Coord? attackOrigin = null)
	{
		var origin = attackOrigin ?? hazard.Center;
		var face = BodyFrame.From(unit).HitFaceFrom(origin);
		var shieldBefore = unit.ShieldPoints[face];
		var hullBefore = unit.HullPoints;

		switch (hazard.Kind)
		{
			case EHazardKind.FlakBurst:
			case EHazardKind.RailgunBurst:
			case EHazardKind.TorpedoBlast:
				ApplyDirectedDamage(hazard, unit, face);
				break;
			default:
				return null;
		}

		var shieldDamage = shieldBefore - unit.ShieldPoints[face];
		var hullDamage = hullBefore - unit.HullPoints;
		if (shieldDamage == 0 && hullDamage == 0)
			return null;

		return new ImpactFacts(
			SourceId: hazard.ActorId,
			TargetId: unit.Id,
			Cause: hazard.Kind,
			Face: face,
			ShieldDamage: shieldDamage,
			HullDamage: hullDamage);
	}

	private static void ApplyDirectedDamage(Hazard hazard, State unit, ESpatialOrientation face)
	{
		if (hazard.Damage <= 0)
			return;

		Defense.ApplyDamage(unit, hazard.Damage, face);
	}
}
