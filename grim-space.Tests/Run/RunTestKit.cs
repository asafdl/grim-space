using GrimSpace.Battle.Encounter;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using BattleUnitType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.Tests.Run;

internal static class RunTestKit
{
	private static int? _fighterSupportSeed;

	public static State CreateFighterSupportRun(bool tutorialsEnabled = false)
	{
		if (_fighterSupportSeed is { } knownSeed)
			return State.CreateNewRun(knownSeed, tutorialsEnabled);

		for (var seed = 0; seed < 1000; seed++)
		{
			var run = State.CreateNewRun(seed, tutorialsEnabled);
			var chassis = run.GetFieldedShipIds()
				.Select(id => run.ItemInventory.Get(id).Spec.Chassis).ToArray();
			if (chassis.SequenceEqual(new[] { BattleUnitType.Fighter, BattleUnitType.RepurposedMiner }))
			{
				_fighterSupportSeed = seed;
				return run;
			}
			run.Dispose();
		}

		throw new InvalidOperationException("No seed produced the required Fighter/Support test squad.");
	}

	public static string StarterFighterShipId(State run) =>
		run.GetFieldedShipIds()
			.Single(id => run.ItemInventory.Get(id).Spec.Chassis == BattleUnitType.Fighter);

	public static string StarterRepurposedMinerShipId(State run) =>
		run.GetFieldedShipIds()
			.Single(id => run.ItemInventory.Get(id).Spec.Chassis == BattleUnitType.RepurposedMiner);

	public static Pilot StarterRangerPilot(State run) =>
		run.PilotRegistry.All.Single(pilot => pilot.ClassId == EOperatorClassId.SpaceRanger);

	public static BattleSpawn StarterFighterBattleSpawn(IEnumerable<BattleSpawn> spawns) =>
		spawns.Single(spawn =>
			spawn.Team == ETeam.Player && spawn.Ship.Spec.Chassis == BattleUnitType.Fighter);

	public static void EnlistFourPilots(State run)
	{
		while (run.PilotRegistry.Assigned.Count < 4)
		{
			var shipId = GrimSpace.Core.Ids.TypedIdGenerator.NextId("fighter");
			if (!run.TryEnlistPlayerShip(new ShipSpawnDeclaration(shipId, BattleUnitType.Fighter)))
				throw new InvalidOperationException("Failed to enlist ship while preparing four assigned pilots.");
		}
	}
}
