using GrimSpace.Battle.Encounter.Generation;
using GrimSpace.Battle.Objectives;
using GrimSpace.Math.Grid;
using GrimSpace.Run.Items;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.Battle.Encounter;

public static class EngagementBattleFactory
{
	//TODO: duplicated grid size, horrible.
	private const int GridSize = 64;
	private const int FieldMargin = 2;

	public static BattleEncounter Create(
		Fleet[] participantFleets,
		Inventory registry,
		int seed,
		string id,
		string initiatorFleetId,
		IReadOnlyDictionary<string, PilotBattleSnapshot>? shipIdToPilotSnapshot = null)
	{
		ArgumentNullException.ThrowIfNull(registry);

		var center = GridSize / 2;
		var fieldCenter = new Coord(center, center, center);
		var spawns = DeploymentPlacement.ForEngagement(
			participantFleets,
			registry,
			seed,
			GridSize,
			initiatorFleetId,
			shipIdToPilotSnapshot);

		return new BattleEncounter
		{
			Id = id,
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
				ReservedCells = AsteroidFieldReservations.PlayerCorridors(spawns),
			}),
		};
	}
}
