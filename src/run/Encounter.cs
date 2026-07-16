// Placeholder until roguelike sector map exists.

using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Run;

public sealed class Encounter
{
	public required int Seed { get; init; }
	public required IReadOnlyList<Spawn> Spawns { get; init; }
	public IReadOnlyList<BoardHazardSpawn> BoardHazards { get; init; } = [];

	public static Encounter DevDefault(int seed = 42, int gridSize = 64)
	{
		var player = new Instance
		{
			Id = "player",
			Type = EType.Fighter,
			Controller = EController.Player,
		};
		var enemy = new Instance
		{
			Id = "enemy",
			Type = EType.Fighter,
			Controller = EController.Enemy,
		};

		var spawns = new[]
		{
			new Spawn { Unit = player, Position = new Coord(30, 32, 32), InitialMomentum = 0 },
			new Spawn { Unit = enemy, Position = new Coord(36, 32, 32), InitialMomentum = 2 },
		};

		return new Encounter
		{
			Seed = seed,
			Spawns = spawns,
			BoardHazards = AsteroidFieldGenerator.Generate(new AsteroidFieldConfig
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
