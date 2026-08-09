// Placeholder until roguelike sector map exists.

using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Run;

public sealed class Encounter
{
	public required int Seed { get; init; }
	public required IReadOnlyList<Spawn> Spawns { get; init; }
	public required EObjective Objective { get; init; }
	public IReadOnlyList<WorldHazardSpawn> WorldHazards { get; init; } = [];

	public static Encounter DevDefault(int seed = 42, int gridSize = 64)
	{
		var player = new Instance
		{
			Type = EType.Fighter,
			Alliance = Alliance.Player,
		};
		var enemy = new Instance
		{
			Type = EType.Patrol,
			Alliance = Alliance.Enemy,
		};

		var (playerSpawn, enemySpawn) = DeploymentPlacement.DevDuel(
			player, enemy, seed, gridSize);
		var spawns = new[] { playerSpawn, enemySpawn };

		return new Encounter
		{
			Seed = seed,
			Spawns = spawns,
			Objective = EObjective.EliminateOpponents,
			WorldHazards = AsteroidFieldGenerator.Generate(new AsteroidFieldConfig
			{
				Seed = seed,
				GridSize = gridSize,
				UnitPositions = spawns.Select(spawn => spawn.Position).ToArray(),
				RegionCenter = RegionCenterBetween(spawns),
			}),
		};
	}

	private static Coord RegionCenterBetween(IReadOnlyList<Spawn> spawns)
	{
		var sum = Coord.Zero;
		foreach (var spawn in spawns)
			sum += spawn.Position;

		return new Coord(sum.X / spawns.Count, sum.Y / spawns.Count, sum.Z / spawns.Count);
	}
}
