// Placeholder until roguelike sector map exists.

using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter.Generation;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Player;
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
		var (playerSpawn, enemySpawn) = DeploymentPlacement.DevDuel(
			EType.Fighter,
			EType.Carrier,
			seed,
			gridSize,
			$"fighter-dev-{seed}",
			$"carrier-dev-{seed}");
		var gunshipSpawn = new BattleSpawn
		{
			Ship = DeploymentPlacement.UpgradeDevShip(
				ShipInstance.FromCatalog($"gunship-dev-{seed}", EType.Gunship)),
			Team = ETeam.Player,
			Position = playerSpawn.Position + new Coord(0, 4, 0),
			Fore = playerSpawn.Fore,
			Dorsal = playerSpawn.Dorsal,
			ExecutionAgent = new UserExecutionAgent(),
		};
		var gooperSpawn = new BattleSpawn
		{
			Ship = DeploymentPlacement.UpgradeDevShip(
				ShipInstance.FromCatalog($"industrial-gooper-dev-{seed}", EType.IndustrialGooper)),
			Team = ETeam.Enemy,
			Position = FlankPosition(enemySpawn, gridSize),
			Fore = enemySpawn.Fore,
			Dorsal = enemySpawn.Dorsal,
			ExecutionAgent = new AiController(),
		};
		var spawns = new[] { playerSpawn, gunshipSpawn, enemySpawn, gooperSpawn };
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
				ReservedCells = AsteroidFieldReservations.PlayerCorridors(spawns),
			}),
		};
	}

	private static Coord FlankPosition(BattleSpawn spawn, int gridSize)
	{
		var lateral = Coord.Cross(spawn.Dorsal, spawn.Fore);
		if (lateral == Coord.Zero)
			lateral = new Coord(1, 0, 0);

		var distance = System.Math.Min(4, gridSize - 1);
		var positive = spawn.Position + lateral * distance;
		if (IsInBounds(positive, gridSize))
			return positive;

		var negative = spawn.Position - lateral * distance;
		return IsInBounds(negative, gridSize) ? negative : spawn.Position;
	}

	private static bool IsInBounds(Coord position, int gridSize) =>
		position.X >= 0 && position.X < gridSize
		&& position.Y >= 0 && position.Y < gridSize
		&& position.Z >= 0 && position.Z < gridSize;
}
