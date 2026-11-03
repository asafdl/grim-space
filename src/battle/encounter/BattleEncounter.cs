// Placeholder until roguelike sector map exists.

using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter.Generation;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Player;
using GrimSpace.Core.Ids;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
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
	public const int DefaultXpDifficulty = 3;
	public int XpDifficulty { get; init; } = DefaultXpDifficulty;
	public IReadOnlyList<BattleHazardSpawn> WorldHazards { get; init; } = [];

	private static readonly EType[] DevPlayerChassis =
	[
		EType.Fighter,
		EType.RepurposedMiner,
		EType.IndustrialGooper,
		EType.Rogue,
	];

	private static readonly EType[] DevEnemyChassis =
	[
		EType.Fighter,
		EType.Carrier,
		EType.IndustrialGooper,
	];

	public static BattleEncounter DevDefault(int seed = 42, int gridSize = 64)
	{
		var (playerAnchor, enemyAnchor) = DeploymentPlacement.DevDuel(
			ShipInstance.FromCatalog($"fighter-dev-{seed}", EType.Fighter),
			ShipInstance.FromCatalog($"fighter-enemy-dev-{seed}", EType.Fighter),
			seed,
			gridSize);

		var playerSpawns = DevClassFormation(seed, playerSide: true, playerAnchor, DevPlayerChassis);
		var enemySpawns = DevClassFormation(seed, playerSide: false, enemyAnchor, DevEnemyChassis);
		var spawns = playerSpawns.Concat(enemySpawns).ToArray();
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

	private static BattleSpawn[] DevClassFormation(
		int seed,
		bool playerSide,
		BattleSpawn anchor,
		EType[] chassisList)
	{
		var lateral = Coord.Cross(anchor.Dorsal, anchor.Fore);
		if (lateral == Coord.Zero)
			lateral = new Coord(0, 1, 0);

		var offsets = new[]
		{
			Coord.Zero,
			lateral * 4,
			lateral * -4,
			anchor.Dorsal * 4,
		};

		var side = playerSide ? "dev" : "enemy-dev";
		var spawns = new BattleSpawn[chassisList.Length];
		for (var index = 0; index < chassisList.Length; index++)
		{
			var chassis = chassisList[index];
			var id = index == 0
				? anchor.Ship.Id
				: $"{UnitTypeSlug.For(chassis)}-{side}-{seed}";
			var ship = index == 0
				? anchor.Ship
				: ShipInstance.FromCatalog(id, chassis);

			spawns[index] = new BattleSpawn
			{
				Ship = ship,
				Pilot = DevFullySkilledPilot(ship.Id, ship.Spec.ClassId),
				Team = playerSide ? ETeam.Player : ETeam.Enemy,
				Position = anchor.Position + offsets[index],
				Fore = anchor.Fore,
				Dorsal = anchor.Dorsal,
				ExecutionAgent = playerSide
					? new UserExecutionAgent()
					: new AiController(),
			};
		}

		return spawns;
	}

	private static PilotBattleSnapshot? DevFullySkilledPilot(string shipId, EOperatorClassId classId) =>
		classId == EOperatorClassId.Unclassified
			? null
			: PilotBattleSnapshot.FromSkills(
				$"pilot-{shipId}",
				PilotSkillCatalog.IdsForOperatorClass(classId));
}
