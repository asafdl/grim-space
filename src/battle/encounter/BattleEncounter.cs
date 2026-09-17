// Placeholder until roguelike sector map exists.

using GrimSpace.Battle.Encounter.Generation;
using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Ids;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.Factions;

namespace GrimSpace.Battle.Encounter;

public sealed class BattleEncounter
{
	public required int Seed { get; init; }

	public required string Id { get; init; }
	public required IReadOnlyList<BattleSpawn> Spawns { get; init; }
	public required EObjective Objective { get; init; }
	public IReadOnlyList<BattleHazardSpawn> WorldHazards { get; init; } = [];

	public static BattleEncounter DevDefault(int seed = 42, int gridSize = 64)
	{
		var player = new Instance
		{
			Type = EType.Fighter,
			Team = ETeam.Player,
		};
		var enemy = new Instance
		{
			Type = EType.Carrier,
			Team = ETeam.Enemy,
		};

		var (playerSpawn, enemySpawn) = DeploymentPlacement.DevDuel(
			player, enemy, seed, gridSize);
		var spawns = new[] { playerSpawn, enemySpawn };
		var fieldMargin = 2;
		var fieldCenter = new Coord(gridSize / 2, gridSize / 2, gridSize / 2);

		return new BattleEncounter
		{
			Id = TypedIdGenerator.NextId("engagement-dev"),
			Seed = seed,
			Spawns = spawns,
			Objective = EObjective.EliminateOpponents,
			WorldHazards = AsteroidFieldGenerator.Generate(new AsteroidFieldConfig
			{
				Seed = seed,
				GridSize = gridSize,
				UnitPositions = spawns.Select(spawn => spawn.Position).ToArray(),
				RegionCenter = fieldCenter,
				RegionHalfExtent = gridSize / 2 - fieldMargin,
				RegionMargin = fieldMargin,
			}),
		};
	}
}
