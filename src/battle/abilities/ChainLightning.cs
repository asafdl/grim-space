using System.Collections.Frozen;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Abilities;

public readonly record struct ChainLightningHop(string TargetId, Coord Position);

public readonly record struct ChainLightningResult(
	IReadOnlySet<Coord> BeamCells,
	IReadOnlyList<ChainLightningHop> Hops);

public static class ChainLightning
{
	public static ChainLightningResult Resolve(
		BattleWorld world,
		string actorId,
		LightningCannonSpec spec,
		ESpatialOrientation mountedOn)
	{
		var state = world.StateOf(actorId);
		var frame = BodyFrame.From(state);
		var burstDirection = frame.Step(mountedOn);
		var lineCells = spec.GetArea(frame.Origin, burstDirection, frame.Fore, frame.Dorsal);
		var geometric = lineCells
			.Where(world.Grid.IsInBounds)
			.ToList();
		var blocked = AbilityArea.ApplyBlocking(
			state.Position,
			geometric.ToFrozenSet(),
			isBlockable: true,
			world.AbilityBlockingCells);
		var orderedBeam = OrderAlongRay(state.Position, burstDirection, blocked);

		var hops = new List<ChainLightningHop>();
		var hitIds = new HashSet<string>(StringComparer.Ordinal);

		foreach (var cell in orderedBeam)
		{
			var opponent = FirstOpponentAt(world, actorId, cell);
			if (opponent is null)
				continue;

			hops.Add(new ChainLightningHop(opponent.State.Id, cell));
			hitIds.Add(opponent.State.Id);
			break;
		}

		for (var chain = 0; chain < spec.MaxChains && hops.Count > 0; chain++)
		{
			var from = hops[^1].Position;
			var next = SelectChainTarget(world, actorId, from, spec.ChainRange, hitIds);
			if (next is null)
				break;

			hops.Add(new ChainLightningHop(next.State.Id, next.State.Position));
			hitIds.Add(next.State.Id);
		}

		return new ChainLightningResult(blocked, hops);
	}

	public static int DamageAtHop(int baseDamage, int hopIndex, int falloffPercentPerHop)
	{
		if (hopIndex <= 0 || falloffPercentPerHop <= 0)
			return baseDamage;

		var factor = 1m;
		for (var hop = 0; hop < hopIndex; hop++)
			factor *= (100 - falloffPercentPerHop) / 100m;

		return (int)System.Math.Round(
			baseDamage * factor,
			MidpointRounding.AwayFromZero);
	}

	internal static bool WouldDamageOpponent(
		BattleWorld world,
		string actorId,
		LightningCannonSpec spec,
		ESpatialOrientation mountedOn) =>
		Resolve(world, actorId, spec, mountedOn).Hops.Count > 0;

	private static IReadOnlyList<Coord> OrderAlongRay(
		Coord origin,
		Coord direction,
		IReadOnlySet<Coord> cells)
	{
		if (cells.Count == 0)
			return [];

		var maxDistance = 0;
		foreach (var cell in cells)
			maxDistance = System.Math.Max(maxDistance, RayDistance(origin, direction, cell));

		var ordered = new List<Coord>(maxDistance);
		for (var distance = 1; distance <= maxDistance; distance++)
		{
			var cell = origin + direction * distance;
			if (cells.Contains(cell))
				ordered.Add(cell);
		}

		return ordered;
	}

	private static int RayDistance(Coord origin, Coord direction, Coord cell)
	{
		if (direction.X != 0)
			return (cell.X - origin.X) / direction.X;
		if (direction.Y != 0)
			return (cell.Y - origin.Y) / direction.Y;
		if (direction.Z != 0)
			return (cell.Z - origin.Z) / direction.Z;
		return 0;
	}

	private static Unit? FirstOpponentAt(BattleWorld world, string actorId, Coord cell)
	{
		Unit? opponent = null;
		foreach (var entry in world.UnitsInCells(actorId, [cell]))
		{
			if (entry.Relation != EUnitRelation.Opponent)
				continue;

			opponent = PickCloserId(opponent, entry.Unit);
		}

		return opponent;
	}

	private static Unit? SelectChainTarget(
		BattleWorld world,
		string actorId,
		Coord from,
		int chainRange,
		IReadOnlySet<string> alreadyHit)
	{
		Unit? best = null;
		var bestDistance = int.MaxValue;
		var actor = world.UnitRegistry.UnitOf(actorId);

		foreach (var unit in world.UnitRegistry.All)
		{
			if (!unit.State.IsAlive
				|| actor.RelationTo(unit) != EUnitRelation.Opponent
				|| alreadyHit.Contains(unit.State.Id))
				continue;

			var distance = from.ManhattanDistanceTo(unit.State.Position);
			if (distance > chainRange || distance == 0)
				continue;

			if (distance < bestDistance
				|| (distance == bestDistance && PickCloserId(best, unit) == unit))
			{
				bestDistance = distance;
				best = unit;
			}
		}

		return best;
	}

	private static Unit PickCloserId(Unit? current, Unit candidate)
	{
		if (current is null)
			return candidate;

		return string.Compare(
			candidate.State.Id,
			current.State.Id,
			StringComparison.Ordinal) < 0
			? candidate
			: current;
	}
}
