using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Encounter.Generation;

/// <summary>
/// Scatters single-cell cryo plumes through the volume spanned by the deployed squads.
/// Layouts are random; one is re-rolled only when plumes skew too far toward one side.
/// </summary>
public sealed class CryoPlumePlacement : IHazardPlacementPass
{
	public const int MinCount = 7;
	public const int MaxCount = 8;
	public const int Padding = 4;
	public const int GridMargin = 2;
	public const int MinShipDistance = 3;
	public const int MinPlumeSpacing = 4;
	public const int MaxSideImbalance = 2;
	private const int LayoutAttempts = 32;
	private const int CellAttemptsPerPlume = 64;

	public void Place(HazardPlacementContext context)
	{
		var player = context.Spawns.Where(spawn => spawn.Team == ETeam.Player).Select(spawn => spawn.Position).ToArray();
		var opponents = context.Spawns.Where(spawn => spawn.Team != ETeam.Player).Select(spawn => spawn.Position).ToArray();
		if (player.Length == 0 || opponents.Length == 0)
			return;

		var playerCenter = Centroid(player);
		var opponentCenter = Centroid(opponents);
		var (min, max) = Bounds(context);
		var count = MinCount + context.Rng.Next(MaxCount - MinCount + 1);

		List<Coord>? best = null;
		var bestImbalance = int.MaxValue;
		for (var attempt = 0; attempt < LayoutAttempts && bestImbalance > MaxSideImbalance; attempt++)
		{
			var layout = RollLayout(context, min, max, count);
			if (layout.Count < count)
				continue;

			var imbalance = SideImbalance(layout, playerCenter, opponentCenter);
			if (imbalance < bestImbalance)
			{
				best = layout;
				bestImbalance = imbalance;
			}
		}

		foreach (var cell in best ?? [])
		{
			context.Claim(new BattleHazardSpawn
			{
				Kind = EBattleHazardKind.CryoPlume,
				Origin = cell,
				Cells = new HashSet<Coord> { cell },
			});
		}
	}

	private static List<Coord> RollLayout(HazardPlacementContext context, Coord min, Coord max, int count)
	{
		var layout = new List<Coord>(count);
		for (var attempt = 0; attempt < count * CellAttemptsPerPlume && layout.Count < count; attempt++)
		{
			var cell = new Coord(
				context.Rng.Next(min.X, max.X + 1),
				context.Rng.Next(min.Y, max.Y + 1),
				context.Rng.Next(min.Z, max.Z + 1));
			if (context.ClaimedCells.Contains(cell)
				|| context.Spawns.Any(spawn => cell.ChebyshevDistanceTo(spawn.Position) < MinShipDistance)
				|| layout.Any(other => cell.ChebyshevDistanceTo(other) < MinPlumeSpacing))
				continue;

			layout.Add(cell);
		}

		return layout;
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
