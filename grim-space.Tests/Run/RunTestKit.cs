using GrimSpace.Battle.Encounter;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using BattleUnitType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.Tests.Run;

internal static class RunTestKit
{
	public static string StarterFighterShipId(State run) =>
		run.GetFieldedShipIds()
			.Single(id => run.ItemInventory.Get(id).Spec.Chassis == BattleUnitType.Fighter);

	public static string StarterRepurposedMinerShipId(State run) =>
		run.GetFieldedShipIds()
			.Single(id => run.ItemInventory.Get(id).Spec.Chassis == BattleUnitType.RepurposedMiner);

	public static Pilot StarterRangerPilot(State run) =>
		run.PilotRegistry.All.Single(pilot =>
			pilot.ClassId == EOperatorClassId.SpaceRanger
			&& pilot.BackgroundId == EPilotBackgroundId.ScrapDrone);

	public static BattleSpawn StarterFighterBattleSpawn(IEnumerable<BattleSpawn> spawns) =>
		spawns.Single(spawn =>
			spawn.Team == ETeam.Player && spawn.Ship.Spec.Chassis == BattleUnitType.Fighter);

	public static void EnlistUntilActivePilotCap(State run)
	{
		while (run.PilotRegistry.Active.Count < RunPilotRegistry.MaxActivePilots)
		{
			var shipId = GrimSpace.Core.Ids.TypedIdGenerator.NextId("fighter");
			if (!run.TryEnlistPlayerShip(new ShipSpawnDeclaration(shipId, BattleUnitType.Fighter)))
				throw new InvalidOperationException("Failed to enlist ship while filling active pilot cap.");
		}
	}
}
