using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Encounter.Generation;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Player;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.Battle.Encounter;

public static class EngagementBattleFactory
{
	private const int GridSize = 64;
	private const int FieldMargin = 2;

	public static BattleEncounter Create(Fleet playerFleet, Fleet enemyFleet, int seed)
	{
		ArgumentNullException.ThrowIfNull(playerFleet);
		ArgumentNullException.ThrowIfNull(enemyFleet);
		ArgumentOutOfRangeException.ThrowIfNegative(seed);
		if (playerFleet.State.Id == enemyFleet.State.Id)
			throw new ArgumentException("An engagement requires two distinct fleets.", nameof(enemyFleet));
		if (playerFleet.Members.Count == 0)
			throw new InvalidOperationException($"Player fleet '{playerFleet.State.Id}' has no members.");
		if (enemyFleet.Members.Count == 0)
			throw new InvalidOperationException($"Enemy fleet '{enemyFleet.State.Id}' has no members.");

		var rng = new Random(seed);
		var center = GridSize / 2;
		var deploySpread = GridSize / 5;
		var playerCenter = center - deploySpread;
		var enemyCenter = center + deploySpread;
		var occupiedPositions = new HashSet<Coord>();
		var spawns = new List<BattleSpawn>();
		AddFleet(
			playerFleet,
			Alliance.Player,
			playerCenter,
			enemyCenter,
			rng,
			occupiedPositions,
			spawns);
		AddFleet(
			enemyFleet,
			Alliance.Enemy,
			enemyCenter,
			playerCenter,
			rng,
			occupiedPositions,
			spawns);

		var fieldCenter = new Coord(center, center, center);
		return new BattleEncounter
		{
			Seed = seed,
			Spawns = spawns,
			Participants =
			[
				new BattleParticipant(
					playerFleet.State.Id,
					playerFleet.Members.Select(member => member.Id).ToArray()),
				new BattleParticipant(
					enemyFleet.State.Id,
					enemyFleet.Members.Select(member => member.Id).ToArray()),
			],
			Objective = EObjective.EliminateOpponents,
			WorldHazards = AsteroidFieldGenerator.Generate(new AsteroidFieldConfig
			{
				Seed = seed,
				GridSize = GridSize,
				UnitPositions = spawns.Select(spawn => spawn.Position).ToArray(),
				RegionCenter = fieldCenter,
				RegionHalfExtent = GridSize / 2 - FieldMargin,
				RegionMargin = FieldMargin,
			}),
		};
	}

	private static void AddFleet(
		Fleet fleet,
		Alliance alliance,
		int deploymentCenter,
		int opposingCenter,
		Random rng,
		HashSet<Coord> occupiedPositions,
		List<BattleSpawn> spawns)
	{
		foreach (var member in fleet.Members)
		{
			Coord position;
			do
			{
				position = new Coord(
					deploymentCenter + rng.Next(-4, 5),
					GridSize / 2 + rng.Next(-8, 9),
					GridSize / 2 + rng.Next(-8, 9));
			}
			while (!occupiedPositions.Add(position));

			var target = new Coord(opposingCenter, GridSize / 2, GridSize / 2);
			spawns.Add(new BattleSpawn
			{
				Unit = new Instance
				{
					Id = member.Id,
					Type = member.Type,
					Alliance = alliance,
				},
				Position = position,
				InitialMomentum = alliance == Alliance.Player ? 0 : rng.Next(0, 3),
				Fore = AxisToward(position, target),
				Dorsal = Coord.Up,
				ExecutionAgent = alliance == Alliance.Player
					? new UserExecutionAgent()
					: new AiController(),
			});
		}
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
