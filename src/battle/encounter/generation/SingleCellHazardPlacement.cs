using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Encounter.Generation;

/// <summary>
/// Places cover near the engagement and other single-cell hazards along the approach.
/// </summary>
public sealed class SingleCellHazardPlacement : IHazardPlacementPass
{
	public const int MinCount = 14;
	public const int MaxCount = 16;
	public const int Padding = 4;
	public const int GridMargin = 2;
	public const int MinShipDistance = 3;
	public const int MinPlumeSpacing = 4;
	public const int MaxSideImbalance = 2;
	private const int LayoutAttempts = 32;
	private const int CellAttemptsPerPlume = 64;
	private const int ApproachRadius = 7;
	private const int EngagementRadius = 8;
	private const int CoverSpacingSquared = 16;
	private const int IonSpacingSquared = 9;
	private readonly EBattleHazardKind _kind;
	private readonly bool _required;

	public SingleCellHazardPlacement(EBattleHazardKind kind, bool required)
	{
		if (kind is not (
			EBattleHazardKind.CryoPlume
			or EBattleHazardKind.IonStorm
			or EBattleHazardKind.AsteroidBelt))
			throw new ArgumentOutOfRangeException(nameof(kind), kind, "Expected a single-cell hazard kind.");
		_kind = kind;
		_required = required;
	}

	public void Place(HazardPlacementContext context)
	{
		var player = context.Spawns.Where(spawn => spawn.Team == ETeam.Player).Select(spawn => spawn.Position).ToArray();
		var opponents = context.Spawns.Where(spawn => spawn.Team != ETeam.Player).Select(spawn => spawn.Position).ToArray();
		if (player.Length == 0 || opponents.Length == 0)
		{
			if (_required)
				throw new InvalidOperationException($"Cannot place {_kind} without both sides of the engagement.");
			return;
		}

		var playerCenter = Centroid(player);
		var opponentCenter = Centroid(opponents);
		var (min, max) = Bounds(context);
		var count = MinCount + context.Rng.Next(MaxCount - MinCount + 1);

		List<Coord>? best = null;
		var bestImbalance = int.MaxValue;
		for (var attempt = 0; attempt < LayoutAttempts && bestImbalance > MaxSideImbalance; attempt++)
		{
			var layout = RollLayout(context, min, max, count, playerCenter, opponentCenter);
			if (layout.Count < count)
				continue;

			var imbalance = SideImbalance(layout, playerCenter, opponentCenter);
			if (imbalance < bestImbalance)
			{
				best = layout;
				bestImbalance = imbalance;
			}
		}

		if (best is null && _required)
			best = RollCompactLayout(context, min, max, count, playerCenter, opponentCenter);

		var cells = best ?? [];
		var firstResistancePercent = _kind == EBattleHazardKind.AsteroidBelt
			? context.Rng.Next(2) == 0
				? AsteroidBelt.LowResistancePercent
				: AsteroidBelt.HighResistancePercent
			: (int?)null;
		for (var index = 0; index < cells.Count; index++)
		{
			var cell = cells[index];
			int? damageResistancePercent = null;
			if (firstResistancePercent is { } firstResistance)
			{
				var isLowResistance = index % 2 == 0
					? firstResistance == AsteroidBelt.LowResistancePercent
					: firstResistance == AsteroidBelt.HighResistancePercent;
				damageResistancePercent = isLowResistance
					? AsteroidBelt.LowResistancePercent
					: AsteroidBelt.HighResistancePercent;
			}

			context.Claim(new BattleHazardSpawn
			{
				Kind = _kind,
				Origin = cell,
				Cells = new HashSet<Coord> { cell },
				DamageResistancePercent = damageResistancePercent,
			});
		}
	}

	private List<Coord> RollLayout(
		HazardPlacementContext context, Coord min, Coord max, int count, Coord playerCenter, Coord opponentCenter)
	{
		var layout = new List<Coord>(count);
		for (var attempt = 0; attempt < count * CellAttemptsPerPlume && layout.Count < count; attempt++)
		{
			var cell = new Coord(
				context.Rng.Next(min.X, max.X + 1),
				context.Rng.Next(min.Y, max.Y + 1),
				context.Rng.Next(min.Z, max.Z + 1));
			if (!IsInTacticalRegion(cell, playerCenter, opponentCenter)
				|| context.ClaimedCells.Contains(cell)
				|| context.Spawns.Any(spawn => cell.ChebyshevDistanceTo(spawn.Position) < MinShipDistance)
				|| layout.Any(other => TooClose(cell, other)))
				continue;

			layout.Add(cell);
		}

		return layout;
	}

	private List<Coord> RollCompactLayout(
		HazardPlacementContext context, Coord min, Coord max, int count, Coord playerCenter, Coord opponentCenter)
	{
		var occupied = context.Spawns.Select(spawn => spawn.Position).ToHashSet();
		var candidates = new List<Coord>();
		for (var x = min.X; x <= max.X; x++)
		for (var y = min.Y; y <= max.Y; y++)
		for (var z = min.Z; z <= max.Z; z++)
		{
			var cell = new Coord(x, y, z);
			if (!occupied.Contains(cell) && !context.ClaimedCells.Contains(cell)
				&& IsInTacticalRegion(cell, playerCenter, opponentCenter))
				candidates.Add(cell);
		}

		var shuffled = candidates.ToArray();
		var best = new List<Coord>();
		for (var attempt = 0; attempt < LayoutAttempts && best.Count < count; attempt++)
		{
			context.Rng.Shuffle(shuffled);
			var layout = new List<Coord>();
			foreach (var cell in shuffled)
			{
				if (layout.Any(other => TooClose(cell, other)))
					continue;
				layout.Add(cell);
				if (layout.Count == count)
					break;
			}
			if (layout.Count > best.Count)
				best = layout;
		}

		if (best.Count == 0)
			throw new InvalidOperationException($"Insufficient unoccupied space to place {_kind} hazards.");
		return best;
	}

	private bool IsInTacticalRegion(Coord cell, Coord playerCenter, Coord opponentCenter)
	{
		if (_kind != EBattleHazardKind.AsteroidBelt)
			return HazardPlacementContext.IsWithinApproach(cell, playerCenter, opponentCenter, ApproachRadius);

		var midpoint = new Coord(
			(playerCenter.X + opponentCenter.X) / 2,
			(playerCenter.Y + opponentCenter.Y) / 2,
			(playerCenter.Z + opponentCenter.Z) / 2);
		var offset = cell - midpoint;
		return Coord.Dot(offset, offset) <= EngagementRadius * EngagementRadius;
	}

	private bool TooClose(Coord cell, Coord other)
	{
		if (_kind == EBattleHazardKind.CryoPlume)
			return cell.ChebyshevDistanceTo(other) < MinPlumeSpacing;

		var offset = cell - other;
		var minimumSquared = _kind == EBattleHazardKind.AsteroidBelt ? CoverSpacingSquared : IonSpacingSquared;
		return Coord.Dot(offset, offset) < minimumSquared;
	}

	private static (Coord Min, Coord Max) Bounds(HazardPlacementContext context)
	{
		var low = GridMargin;
		var high = context.GridSize - GridMargin - 1;
		var positions = context.Spawns.Select(spawn => spawn.Position).ToArray();
		return (
			new Coord(
				System.Math.Max(low, positions.Min(p => p.X) - Padding),
				System.Math.Max(low, positions.Min(p => p.Y) - Padding),
				System.Math.Max(low, positions.Min(p => p.Z) - Padding)),
			new Coord(
				System.Math.Min(high, positions.Max(p => p.X) + Padding),
				System.Math.Min(high, positions.Max(p => p.Y) + Padding),
				System.Math.Min(high, positions.Max(p => p.Z) + Padding)));
	}

	public static int SideImbalance(IEnumerable<Coord> plumes, Coord playerCenter, Coord opponentCenter)
	{
		var nearPlayer = 0;
		var nearOpponent = 0;
		foreach (var plume in plumes)
		{
			var toPlayer = DistanceSquared(plume, playerCenter);
			var toOpponent = DistanceSquared(plume, opponentCenter);
			if (toPlayer < toOpponent)
				nearPlayer++;
			else if (toOpponent < toPlayer)
				nearOpponent++;
		}

		return System.Math.Abs(nearPlayer - nearOpponent);
	}

	public static Coord Centroid(IReadOnlyList<Coord> positions) =>
		new(
			(int)System.Math.Round(positions.Average(p => p.X)),
			(int)System.Math.Round(positions.Average(p => p.Y)),
			(int)System.Math.Round(positions.Average(p => p.Z)));

	private static int DistanceSquared(Coord a, Coord b)
	{
		var delta = a - b;
		return Coord.Dot(delta, delta);
	}
}
