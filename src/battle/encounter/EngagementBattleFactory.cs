using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Encounter.Generation;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Player;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Encounter;

public static class EngagementBattleFactory
{
	private const int GridSize = 64;
	private const int PatrolCount = 3;
	private const int FieldMargin = 2;

	public static BattleEncounter Create(Party playerParty, int seed)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(seed);
		if (playerParty.Members.Count == 0)
			throw new InvalidOperationException("Player party must contain at least one ship.");

		var rng = new Random(seed);
		var playerInstance = playerParty.Members[0];
		var center = GridSize / 2;
		var deploySpread = GridSize / 5;
		var playerPosition = new Coord(center - deploySpread, center, center);
		var spawns = new List<BattleSpawn>
		{
			new()
			{
				Unit = playerInstance,
				Position = playerPosition,
				InitialMomentum = 0,
				Fore = Coord.Forward,
				Dorsal = Coord.Up,
				ExecutionAgent = new UserExecutionAgent(),
			},
		};

		var enemyCenter = center + deploySpread;
		for (var i = 0; i < PatrolCount; i++)
		{
			var patrolPosition = new Coord(
				enemyCenter + rng.Next(-4, 5),
				rng.Next(center - 8, center + 9),
				rng.Next(center - 8, center + 9));
			spawns.Add(new BattleSpawn
			{
				Unit = new Instance
				{
					Type = EType.Patrol,
					Alliance = Alliance.Enemy,
				},
				Position = patrolPosition,
				InitialMomentum = rng.Next(0, 3),
				Fore = AxisToward(patrolPosition, playerPosition),
				Dorsal = Coord.Up,
				ExecutionAgent = new AiController(),
			});
		}

		var fieldCenter = new Coord(center, center, center);
		return new BattleEncounter
		{
			Seed = seed,
			Spawns = spawns,
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
