using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Player;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Units;
using BattleUnitType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.Battle.Encounter.Generation;

public static class DeploymentPlacement
{
	private const int Margin = 4;
	private const int DeploySpreadDivisor = 5;
	private const int EnemySpreadBand = 4;
	private const int LaneHalfBand = 8;

	public static (BattleSpawn Player, BattleSpawn Enemy) DevDuel(
		BattleUnitType playerChassis,
		BattleUnitType enemyChassis,
		int seed,
		int gridSize,
		string? playerId = null,
		string? enemyId = null)
	{
		var playerShip = ShipInstance.FromCatalog(
			playerId ?? "dev-player",
			playerChassis);
		var enemyShip = ShipInstance.FromCatalog(
			enemyId ?? "dev-enemy",
			enemyChassis);
		return DevDuel(playerShip, enemyShip, seed, gridSize);
	}

	public static List<BattleSpawn> ForEngagement(
		IReadOnlyList<Fleet> participantFleets,
		RunShipRegistry registry,
		int seed,
		int gridSize)
	{
		ArgumentNullException.ThrowIfNull(registry);
		ArgumentNullException.ThrowIfNull(participantFleets);
		ArgumentOutOfRangeException.ThrowIfLessThan(gridSize, 1);

		var playerMembers = participantFleets
			.Where(fleet => fleet.State.Faction == EFaction.Player)
			.SelectMany(fleet => fleet.Members.Select(member => (fleet, member)))
			.ToList();
		var enemyMembers = participantFleets
			.Where(fleet => fleet.State.Faction != EFaction.Player)
			.SelectMany(fleet => fleet.Members.Select(member => (fleet, member)))
			.ToList();

		var center = gridSize / 2;
		var deploySpread = gridSize / DeploySpreadDivisor;
		var playerAnchor = new Coord(center - deploySpread, center, center);
		var enemyAnchor = playerMembers.Count > 0
			? PickEngagementEnemyPosition(seed, gridSize, playerAnchor)
			: new Coord(center + deploySpread, center, center);

		var rng = new Random(seed);
		var occupied = new HashSet<Coord>();
		var spawns = new List<BattleSpawn>();
		PlaceTeam(
			playerMembers,
			ETeam.Player,
			playerAnchor,
			enemyAnchor,
			rng,
			gridSize,
			occupied,
			registry,
			spawns);
		PlaceTeam(
			enemyMembers,
			ETeam.Enemy,
			enemyAnchor,
			playerAnchor,
			rng,
			gridSize,
			occupied,
			registry,
			spawns);
		return spawns;
	}

	public static (BattleSpawn Player, BattleSpawn Enemy) DevDuel(
		ShipInstance playerShip,
		ShipInstance enemyShip,
		int seed,
		int gridSize)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(gridSize, 1);
		var center = gridSize / 2;
		var deploySpread = gridSize / DeploySpreadDivisor;
		var playerPosition = new Coord(center - deploySpread, center, center);
		var enemyPosition = PickEnemyPosition(seed, gridSize, playerPosition);
		var dorsal = Coord.Up;
		var playerFore = AxisToward(playerPosition, enemyPosition);
		var enemyFore = AxisToward(enemyPosition, playerPosition);

		return (
			new BattleSpawn
			{
				Ship = playerShip,
				Team = ETeam.Player,
				Position = playerPosition,
				Fore = playerFore,
				Dorsal = dorsal,
				ExecutionAgent = new UserExecutionAgent(),
			},
			new BattleSpawn
			{
				Ship = enemyShip,
				Team = ETeam.Enemy,
				Position = enemyPosition,
				Fore = enemyFore,
				Dorsal = dorsal,
				ExecutionAgent = new AiController(),
			});
	}

	private static void PlaceTeam(
		IReadOnlyList<(Fleet Fleet, FleetMember Member)> members,
		ETeam team,
		Coord anchor,
		Coord faceToward,
		Random rng,
		int gridSize,
		HashSet<Coord> occupied,
		RunShipRegistry registry,
		List<BattleSpawn> spawns)
	{
		if (members.Count == 0)
			return;

		var dorsal = Coord.Up;
		var teamFore = AxisToward(anchor, faceToward);
		var basis = GridBasis.From(teamFore, dorsal, Coord.Cross(dorsal, teamFore));

		for (var index = 0; index < members.Count; index++)
		{
			var (fleet, member) = members[index];
			var position = index == 0 && occupied.Add(anchor)
				? anchor
				: PickFormationCell(rng, anchor, basis, occupied, gridSize);
			spawns.Add(new BattleSpawn
			{
				Ship = registry.Get(member.Id).Clone(),
				Team = team,
				Position = position,
				Fore = AxisToward(position, faceToward),
				Dorsal = dorsal,
				ExecutionAgent = fleet.State.Faction == EFaction.Player
					? new UserExecutionAgent()
					: new AiController(),
			});
		}
	}

	private static Coord PickFormationCell(
		Random rng,
		Coord anchor,
		GridBasis basis,
		HashSet<Coord> occupied,
		int gridSize)
	{
		for (var attempt = 0; attempt < 64; attempt++)
		{
			var candidate = basis.ToWorldCell(
				anchor,
				rng.Next(-2, 3),
				rng.Next(-3, 4),
				rng.Next(-2, 3));
			if (!IsInBounds(candidate, gridSize) || !occupied.Add(candidate))
				continue;

			return candidate;
		}

		throw new InvalidOperationException("Could not find an open deployment cell.");
	}

	private static bool IsInBounds(Coord cell, int gridSize) =>
		cell.X >= 0 && cell.X < gridSize
		&& cell.Y >= 0 && cell.Y < gridSize
		&& cell.Z >= 0 && cell.Z < gridSize;

	private static Coord PickEngagementEnemyPosition(int seed, int gridSize, Coord playerPosition)
	{
		var rng = new Random(seed);
		var half = gridSize / 2;
		var center = gridSize / 2;
		var deploySpread = gridSize / DeploySpreadDivisor;
		var enemyCenter = center + deploySpread;
		var minX = playerPosition.X < half
			? enemyCenter - EnemySpreadBand
			: Margin;
		var maxX = playerPosition.X < half
			? enemyCenter + EnemySpreadBand
			: half - Margin - 1;

		return new Coord(
			NextInBounds(rng, minX, maxX, gridSize),
			NextInBounds(rng, center - LaneHalfBand, center + LaneHalfBand, gridSize),
			NextInBounds(rng, center - LaneHalfBand, center + LaneHalfBand, gridSize));
	}

	private static Coord PickEnemyPosition(int seed, int gridSize, Coord playerPosition)
	{
		var rng = new Random(seed);
		var half = gridSize / 2;
		var center = gridSize / 2;

		var deploySpread = gridSize / DeploySpreadDivisor;
		var enemyCenter = center + deploySpread;
		var minX = playerPosition.X < half
			? enemyCenter - EnemySpreadBand
			: Margin;
		var maxX = playerPosition.X < half
			? enemyCenter + EnemySpreadBand
			: half - Margin - 1;

		return new Coord(
			NextInBounds(rng, minX, maxX, gridSize),
			NextInBounds(rng, center - LaneHalfBand, center + LaneHalfBand, gridSize),
			NextInBounds(rng, center - LaneHalfBand, center + LaneHalfBand, gridSize));
	}

	private static int NextInBounds(Random rng, int min, int max, int gridSize)
	{
		var last = gridSize - 1;
		var boundedMin = System.Math.Clamp(min, 0, last);
		var boundedMax = System.Math.Clamp(max, boundedMin, last);
		return rng.Next(boundedMin, boundedMax + 1);
	}

	private static Coord AxisToward(Coord from, Coord to)
	{
		var delta = to - from;
		var ax = System.Math.Abs(delta.X);
		var ay = System.Math.Abs(delta.Y);
		var az = System.Math.Abs(delta.Z);

		if (ax >= ay && ax >= az)
			return new Coord(System.Math.Sign(delta.X), 0, 0);

		if (ay >= ax && ay >= az)
			return new Coord(0, System.Math.Sign(delta.Y), 0);

		return new Coord(0, 0, System.Math.Sign(delta.Z));
	}
}
