using GrimSpace.Battle.Encounter.Generation;
using GrimSpace.Battle.Objectives;
using GrimSpace.Math.Grid;
using GrimSpace.Run.Items;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.Battle.Encounter;

public static class EngagementBattleFactory
{
	public static BattleEncounter Create(
		Fleet[] participantFleets,
		Inventory registry,
		int seed,
		string id,
		string initiatorFleetId,
		IReadOnlyDictionary<string, PilotBattleSnapshot>? shipIdToPilotSnapshot = null,
		bool canonicalLayout = false,
		bool tutorialScriptedSpawn = false,
		IReadOnlySet<string>? playerDeployShipIds = null)
	{
		ArgumentNullException.ThrowIfNull(registry);

		var spawns = DeploymentPlacement.ForEngagement(
			participantFleets,
			registry,
			seed,
			BattleEncounter.DefaultGridSize,
			initiatorFleetId,
			shipIdToPilotSnapshot,
			canonicalLayout,
			tutorialScriptedSpawn,
			playerDeployShipIds);

		BattleEngagementComposition.EnsurePlayable(spawns, id);

		return new BattleEncounter
		{
			Id = id,
			Seed = seed,
			Spawns = spawns,
			Objective = EObjective.EliminateOpponents,
			WorldHazards = tutorialScriptedSpawn
				? []
				: HazardPlacement.Generate(seed, BattleEncounter.DefaultGridSize, spawns),
		};
	}
}
