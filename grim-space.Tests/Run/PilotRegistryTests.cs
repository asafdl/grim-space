using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Ids;
using GrimSpace.Run;
using GrimSpace.Run.Persistence;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.Pilots;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using BattleUnitType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class PilotRegistryTests
{
	[Fact]
	public void CreateNewRun_SelectsTwoDistinctSeededClassesWithMatchingT0Ships()
	{
		var seen = new HashSet<EOperatorClassId>();
		for (var seed = 0; seed < 12; seed++)
		{
			using var first = State.CreateNewRun(seed);
			using var second = State.CreateNewRun(seed);
			var pilots = first.PilotRegistry.Assigned;
			Assert.Equal(2, pilots.Count);
			Assert.Equal(2, pilots.Select(pilot => pilot.ClassId).Distinct().Count());
			Assert.Equal(
				pilots.Select(pilot => pilot.ClassId),
				second.PilotRegistry.Assigned.Select(pilot => pilot.ClassId));
			foreach (var pilot in pilots)
			{
				seen.Add(pilot.ClassId);
				var ship = first.ItemInventory.Get(pilot.AssignedShipId!);
				Assert.Equal(pilot.ClassId, ship.Spec.ClassId);
				Assert.Equal(
					ShipCatalog.LoadoutForTier(ship.Spec.Chassis, EShipGearTier.T0).InstalledAbilities,
					ship.Loadout.InstalledAbilities);
				Assert.Equal(0, pilot.ExperiencePoints);
				Assert.Empty(pilot.LearnedSkills);
			}
		}

		Assert.Equal(
			new[] { EOperatorClassId.SpaceRanger, EOperatorClassId.Support, EOperatorClassId.Tank, EOperatorClassId.Rogue }.Order(),
			seen.Order());
	}

	[Fact]
	public void CreateNewRun_RegistersPilotForStarterShip()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		Assert.Equal(2, run.GetFieldedShipIds().Count);
		var shipId = RunTestKit.StarterFighterShipId(run);

		Assert.True(run.PilotRegistry.TryGetForShip(shipId, out var pilot));
		Assert.StartsWith("pilot-", pilot.Id, StringComparison.Ordinal);
		Assert.False(string.IsNullOrWhiteSpace(pilot.Name));
		Assert.Contains(pilot.PortraitId, Pilot.PortraitPool);
		Assert.Equal(shipId, pilot.AssignedShipId);
		Assert.Equal(2, run.PilotRegistry.Assigned.Count);
		Assert.Equal(pilot, RunTestKit.StarterRangerPilot(run));
		Assert.Equal(PilotProgression.LevelFor(0), pilot.ClaimedLevel);
		Assert.Empty(pilot.LearnedSkills);
		Assert.Contains(PilotBackgroundCatalog.All, background => background.Id == pilot.BackgroundId);
		Assert.Equal(EPilotQuirkId.Charismatic, Assert.Single(pilot.PositiveQuirkIds));
		Assert.Equal(EPilotQuirkId.NeuroDrugAddict, Assert.Single(pilot.NegativeQuirkIds));
	}

	[Fact]
	public void CreateNewRun_FieldedRosterIncludesSupportEngineerOnRepurposedMinerT0()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var minerShipId = RunTestKit.StarterRepurposedMinerShipId(run);
		var ship = run.ItemInventory.Get(minerShipId);

		Assert.Equal(BattleUnitType.RepurposedMiner, ship.Spec.Chassis);
		Assert.Equal(
			ShipCatalog.LoadoutForTier(BattleUnitType.RepurposedMiner, EShipGearTier.T0).BaseHullPoints,
			ship.Loadout.BaseHullPoints);
		Assert.True(run.PilotRegistry.TryGetForShip(minerShipId, out var pilot));
		Assert.Equal(EOperatorClassId.Support, pilot.ClassId);
		Assert.Equal(minerShipId, pilot.AssignedShipId);
		Assert.Equal(2, run.PilotRegistry.Assigned.Count);
	}

	[Fact]
	public void Enlist_AddsSecondPilotWhilePartyOrderStaysShipOrdered()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var fighterShipId = RunTestKit.StarterFighterShipId(run);
		var minerShipId = RunTestKit.StarterRepurposedMinerShipId(run);
		var thirdShipId = TypedIdGenerator.NextId("fighter");
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(thirdShipId, BattleUnitType.Fighter)));

		Assert.Equal([fighterShipId, minerShipId, thirdShipId], run.GetFieldedShipIds());
		Assert.True(run.PilotRegistry.TryGetForShip(fighterShipId, out var firstPilot));
		Assert.True(run.PilotRegistry.TryGetForShip(thirdShipId, out var secondPilot));
		Assert.NotEqual(firstPilot.Id, secondPilot.Id);
	}

	[Fact]
	public void TryAssignPilotToShip_SwapsActivePilotsWhilePreservingFieldedShipOrder()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var fighterShipId = RunTestKit.StarterFighterShipId(run);
		var minerShipId = RunTestKit.StarterRepurposedMinerShipId(run);
		var thirdShipId = TypedIdGenerator.NextId("fighter");
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(thirdShipId, BattleUnitType.Fighter)));
		Assert.True(run.PilotRegistry.TryGetForShip(fighterShipId, out var firstPilot));
		Assert.True(run.PilotRegistry.TryGetForShip(thirdShipId, out var secondPilot));

		Assert.True(run.TryAssignPilotToShip(secondPilot.Id, fighterShipId));

		Assert.Equal([fighterShipId, minerShipId, thirdShipId], run.GetFieldedShipIds());
		Assert.True(run.PilotRegistry.TryGetForShip(fighterShipId, out var reassignedFirst));
		Assert.True(run.PilotRegistry.TryGetForShip(thirdShipId, out var reassignedSecond));
		Assert.Equal(secondPilot.Id, reassignedFirst.Id);
		Assert.Equal(firstPilot.Id, reassignedSecond.Id);
		Assert.Equal(
			run.GetFieldedShipIds(),
			run.StarSystem.Map.FleetRegistry
				.FleetOf(State.PlayerFleetUnitId)
				.Members
				.Select(member => member.Id)
				.ToArray());
	}

	[Fact]
	public void TryAssignPilotToShip_ReplacesActivePilotWithUnassignedPilot()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var shipId = RunTestKit.StarterFighterShipId(run);
		Assert.True(run.PilotRegistry.TryGetForShip(shipId, out var originalPilot));
		var replacement = Pilot.CreateForShip("reserve-ship");
		run.PilotRegistry.Register(replacement);
		run.PilotRegistry.Unassign(replacement.Id);

		Assert.True(run.TryAssignPilotToShip(replacement.Id, shipId));

		Assert.Equal(replacement.Id, run.PilotRegistry.Assigned.Single(active => active.Id == replacement.Id).Id);
		Assert.Equal(shipId, RunTestKit.StarterFighterShipId(run));
		Assert.True(run.PilotRegistry.TryGet(replacement.Id, out var assignedReplacement));
		Assert.Equal(shipId, assignedReplacement.AssignedShipId);
		Assert.True(run.PilotRegistry.TryGet(originalPilot.Id, out var replacedPilot));
		Assert.Null(replacedPilot.AssignedShipId);
	}

	[Fact]
	public void TryAssignPilotToShip_KeepsActivePilotWhenMovingToEmptyOwnedShip()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var starterShipId = RunTestKit.StarterFighterShipId(run);
		Assert.True(run.PilotRegistry.TryGetForShip(starterShipId, out var pilot));
		var emptyShipId = TypedIdGenerator.NextId("fighter");
		Assert.True(run.TryAcquireOwnedShip(
			new ShipSpawnDeclaration(emptyShipId, BattleUnitType.Fighter, EShipGearTier.T1)));
		var activeBefore = run.PilotRegistry.Assigned.Count;

		Assert.True(run.TryAssignPilotToShip(pilot.Id, emptyShipId));

		Assert.True(run.PilotRegistry.TryGet(pilot.Id, out var moved));
		Assert.Equal(emptyShipId, moved.AssignedShipId);
		Assert.Equal(activeBefore, run.PilotRegistry.Assigned.Count);
		Assert.Contains(emptyShipId, run.GetFieldedShipIds());
		Assert.DoesNotContain(starterShipId, run.GetFieldedShipIds());
		Assert.False(run.PilotRegistry.TryGetForShip(starterShipId, out _));
	}

	[Fact]
	public void TryAssignPilotToShip_FieldsReservePilotOntoEmptyShipWhenSquadHasRoom()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		Assert.Equal(2, run.PilotRegistry.Assigned.Count);
		var emptyShipId = TypedIdGenerator.NextId("fighter");
		Assert.True(run.TryAcquireOwnedShip(
			new ShipSpawnDeclaration(emptyShipId, BattleUnitType.Fighter, EShipGearTier.T1)));
		var reserve = Pilot.CreateForShip("reserve-for-empty");
		run.PilotRegistry.Register(reserve);
		run.PilotRegistry.Unassign(reserve.Id);

		Assert.True(run.TryAssignPilotToShip(reserve.Id, emptyShipId));

		Assert.True(run.PilotRegistry.TryGet(reserve.Id, out var fielded));
		Assert.Equal(emptyShipId, fielded.AssignedShipId);
		Assert.Equal(3, run.PilotRegistry.Assigned.Count);
		Assert.Contains(emptyShipId, run.GetFieldedShipIds());
	}

	[Fact]
	public void TryAssignPilotToShip_RejectsUnknownPilotOrUnownedShip()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pilot = RunTestKit.StarterRangerPilot(run);

		Assert.False(run.TryAssignPilotToShip("missing-pilot", pilot.AssignedShipId!));
		Assert.False(run.TryAssignPilotToShip(pilot.Id, "missing-ship"));
		Assert.False(run.TryAssignPilotToShip(
			"missing-pilot",
			pilot.AssignedShipId!,
			out var missingPilotReason));
		Assert.Equal(EPilotAssignmentFailureReason.PilotNotFound, missingPilotReason);
		Assert.False(run.TryAssignPilotToShip(
			pilot.Id,
			"missing-ship",
			out var unownedShipReason));
		Assert.Equal(EPilotAssignmentFailureReason.ShipNotOwned, unownedShipReason);
	}

	[Fact]
	public void TryAssignPilotToShip_RejectsUnclassifiedShipWithoutChangingRoster()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pilot = RunTestKit.StarterRangerPilot(run);
		var shipId = "unclassified-reserve";
		Assert.True(run.TryAcquireOwnedShip(new ShipSpawnDeclaration(shipId, BattleUnitType.Gunship)));
		var fieldedBefore = run.GetFieldedShipIds().ToArray();
		var activeBefore = run.PilotRegistry.Assigned.ToArray();

		Assert.False(run.TryAssignPilotToShip(
			pilot.Id,
			shipId,
			out var failureReason));

		Assert.Equal(EPilotAssignmentFailureReason.IncompatibleClass, failureReason);
		Assert.Equal(fieldedBefore, run.GetFieldedShipIds());
		Assert.Equal(activeBefore, run.PilotRegistry.Assigned);
		Assert.Equal(run.ItemInventory.Get(shipId).Id, shipId);
		Assert.Equal(run.ItemInventory.Get(fieldedBefore[0]).Id, pilot.AssignedShipId);
	}

	[Fact]
	public void TryAssignPilotToShip_RejectsSwapWhenEitherResultingPairingIsIncompatible()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var fighterShipId = RunTestKit.StarterFighterShipId(run);
		var minerShipId = "unclassified-swap";
		Assert.True(run.TryAcquireOwnedShip(new ShipSpawnDeclaration(minerShipId, BattleUnitType.RepurposedMiner)));
		var pilot = RunTestKit.StarterRangerPilot(run);
		var reservePilot = Pilot.CreateForShip("reserve-id");
		run.PilotRegistry.Register(reservePilot);
		run.PilotRegistry.Unassign(reservePilot.Id);
		var assignmentsBefore = run.PilotRegistry.All
			.OrderBy(candidate => candidate.Id, StringComparer.Ordinal)
			.Select(candidate => (candidate.Id, candidate.AssignedShipId))
			.ToArray();

		Assert.False(run.TryAssignPilotToShip(pilot.Id, minerShipId));
		Assert.Equal(
			assignmentsBefore,
			run.PilotRegistry.All
				.OrderBy(candidate => candidate.Id, StringComparer.Ordinal)
				.Select(candidate => (candidate.Id, candidate.AssignedShipId))
				.ToArray());
		Assert.Equal(fighterShipId, pilot.AssignedShipId);
	}

	[Fact]
	public void RegistryDirectAssignmentRejectsIncompatibleShip()
	{
		var registry = new RunPilotRegistry();
		var inventory = new GrimSpace.Run.Items.Inventory();
		const string playerFleet = State.PlayerFleetUnitId;
		var pilot = Pilot.CreateForShip("unclassified-ship");
		inventory.Register(
			ShipInstance.FromCatalog("unclassified-ship", EType.RepurposedMiner),
			playerFleet);
		registry.Register(pilot);
		registry.Unassign(pilot.Id);

		Assert.Throws<InvalidOperationException>(() =>
			registry.AssignShip(pilot.Id, "unclassified-ship", inventory, playerFleet));
		Assert.Empty(registry.Assigned);
	}

	[Fact]
	public void RegistrySwapAssignmentsValidatesBothPairingsBeforeMutation()
	{
		var registry = new RunPilotRegistry();
		var inventory = new GrimSpace.Run.Items.Inventory();
		const string playerFleet = State.PlayerFleetUnitId;
		inventory.Register(ShipInstance.FromCatalog("fighter-swap", EType.Fighter), playerFleet);
		inventory.Register(ShipInstance.FromCatalog("miner-swap", EType.RepurposedMiner), playerFleet);
		var fighterPilot = Pilot.CreateStarterForShip("fighter-swap");
		var minerPilot = Pilot.CreateForShip("miner-swap", EOperatorClassId.Support);
		registry.Register(fighterPilot);
		registry.Register(minerPilot);
		var before = registry.All
			.OrderBy(candidate => candidate.Id, StringComparer.Ordinal)
			.Select(candidate => (candidate.Id, candidate.AssignedShipId))
			.ToArray();

		Assert.Throws<InvalidOperationException>(() =>
			registry.SwapAssignments(fighterPilot.Id, minerPilot.Id, inventory, playerFleet));
		Assert.Equal(
			before,
			registry.All
				.OrderBy(candidate => candidate.Id, StringComparer.Ordinal)
				.Select(candidate => (candidate.Id, candidate.AssignedShipId))
				.ToArray());
	}

	[Fact]
	public void Update_ReplacesRegisteredPilot()
	{
		var registry = new RunPilotRegistry();
		var pilot = Pilot.CreateForShip("fighter-test-ship");
		registry.Register(pilot);
		var leveled = PilotProgression.AddExperience(pilot, 150);

		registry.Update(leveled);

		Assert.True(registry.TryGet(pilot.Id, out var stored));
		Assert.Equal(150, stored.ExperiencePoints);
	}

	[Fact]
	public void Update_ThrowsWhenPilotNotRegistered()
	{
		var registry = new RunPilotRegistry();
		var pilot = Pilot.CreateForShip("fighter-test-ship");

		Assert.Throws<KeyNotFoundException>(() => registry.Update(pilot));
	}

	[Fact]
	public void LearnedCount_CountsLearnedSkillAcrossPilots()
	{
		var registry = new RunPilotRegistry();
		for (var index = 0; index < 2; index++)
		{
			var pilot = PilotProgression
				.AddExperience(
					Pilot.FromRecruitProfile(
						$"fighter-{index}",
						new PilotRecruitProfile(
							"Test",
							"pilot_01",
							EPilotBackgroundId.ScrapDrone)),
					100)
				.LearnPilotSkill(EPilotSkillId.DronesYearnForMines);
			registry.Register(pilot);
		}

		Assert.Equal(2, registry.LearnedCount(EPilotSkillId.DronesYearnForMines));
		Assert.Equal(0, registry.LearnedCount(EPilotSkillId.IKnowABot));
	}

	[Fact]
	public void RemoveForShip_DropsPilotAssignedToThatShip()
	{
		var registry = new RunPilotRegistry();
		var pilot = Pilot.CreateForShip("fighter-test-ship");
		registry.Register(pilot);

		registry.RemoveForShip("fighter-test-ship");

		Assert.False(registry.TryGet(pilot.Id, out _));
		Assert.False(registry.TryGetForShip("fighter-test-ship", out _));
	}

	[Fact]
	public void Snapshot_RoundTripsPartyPilotsAndAssignments()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var secondShipId = TypedIdGenerator.NextId("fighter");
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(secondShipId, BattleUnitType.Fighter)));
		var registry = PersistenceRegistry.CreateDefault();

		var snapshot = run.CaptureSnapshot(registry);
		using var restored = State.FromSnapshot(snapshot, registry);

		Assert.Equal(run.GetFieldedShipIds(), restored.GetFieldedShipIds());
		foreach (var shipId in run.GetFieldedShipIds())
		{
			Assert.True(run.PilotRegistry.TryGetForShip(shipId, out var original));
			Assert.True(restored.PilotRegistry.TryGetForShip(shipId, out var roundTripped));
			Assert.Equal(original.Id, roundTripped.Id);
			Assert.Equal(original.Name, roundTripped.Name);
			Assert.Equal(original.PortraitId, roundTripped.PortraitId);
			Assert.Equal(original.AssignedShipId, roundTripped.AssignedShipId);
			Assert.Equal(original.ExperiencePoints, roundTripped.ExperiencePoints);
			Assert.Equal(original.ClaimedLevel, roundTripped.ClaimedLevel);
			Assert.Equal(original.LearnedSkills, roundTripped.LearnedSkills);
		}
	}

	[Fact]
	public void Snapshot_RestoresPlayerStrategicActionPipeline()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pilot = RunTestKit.StarterRangerPilot(run);
		foreach (var other in run.PilotRegistry.All.Where(p => p.Id != pilot.Id).ToArray())
			Assert.True(run.TryDismissPilot(other.Id));
		// Scrap Drone minor scrap + Drones Yearn For Mines must both be present for the 120 scrap assert.
		run.PilotRegistry.Update(
			Pilot.FromPersistence(pilot.ToPersistence() with { BackgroundId = EPilotBackgroundId.ScrapDrone }));
		Assert.True(run.PilotRegistry.TryGet(pilot.Id, out pilot));
		run.PilotRegistry.Update(PilotProgression.AddExperience(pilot, 100));
		Assert.True(run.TryLearnPilotSkill(pilot.Id, EPilotSkillId.DronesYearnForMines));
		var registry = PersistenceRegistry.CreateDefault();

		var snapshot = run.CaptureSnapshot(registry);
		using var restored = State.FromSnapshot(snapshot, registry);
		var runtime = restored.StarSystem.RuntimeFor(State.PlayerFleetUnitId);
		var action = new ResolveEngagementAction(
			State.PlayerFleetUnitId,
			new BattleOutcome("battle", EBattleResult.Win, []),
			[],
			ResourceBundle.Empty);
		var modified = Assert.IsType<AwardEngagementLootEffect>(
			Assert.Single(runtime.ActionEffects.Apply(
				action,
				restored.StarSystem.Map,
				runtime,
				[new AwardEngagementLootEffect(
					[new LootRoll(
						"miner",
						BattleUnitType.RepurposedMiner,
						ResourceBundle.Of(ResourceId.ScrapAlloy, 100))],
					ResourceBundle.Of(ResourceId.ScrapAlloy, 100))])));

		Assert.True(modified.Total.TryGet(ResourceId.ScrapAlloy, out var scrap));
		Assert.Equal(120, scrap);
	}

	[Fact]
	public void Snapshot_RoundTripsExperiencePoints()
	{
		var registry = new RunPilotRegistry();
		var pilot = PilotProgression.AddExperience(Pilot.CreateForShip("fighter-test-ship"), 175);
		registry.Register(pilot);

		var dto = Assert.Single(registry.CaptureSnapshot());
		var restoredRegistry = new RunPilotRegistry();
		restoredRegistry.RestoreSnapshot([dto]);

		Assert.True(restoredRegistry.TryGet(pilot.Id, out var restored));
		Assert.Equal(175, restored.ExperiencePoints);
		Assert.Equal(pilot.BackgroundId, restored.BackgroundId);
		Assert.Equal(pilot.PositiveQuirkIds, restored.PositiveQuirkIds);
		Assert.Equal(pilot.NegativeQuirkIds, restored.NegativeQuirkIds);
	}

	[Fact]
	public void RegenerateMap_PreservesPartyAndPilots()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var secondShipId = TypedIdGenerator.NextId("fighter");
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(secondShipId, BattleUnitType.Fighter)));
		var pilotsBefore = run.GetFieldedShipIds()
			.Select(shipId =>
			{
				Assert.True(run.PilotRegistry.TryGetForShip(shipId, out var pilot));
				return pilot;
			})
			.ToArray();

		run.RegenerateMap(77);

		Assert.Equal(pilotsBefore.Length, run.GetFieldedShipIds().Count);
		foreach (var pilot in pilotsBefore)
		{
			Assert.Contains(pilot.AssignedShipId, run.GetFieldedShipIds());
			Assert.True(run.PilotRegistry.TryGet(pilot.Id, out var restored));
			Assert.Equal(pilot.Name, restored.Name);
			Assert.Equal(pilot.PortraitId, restored.PortraitId);
		}
	}

	[Fact]
	public void FromSnapshot_RestoresLastDeploymentOrder()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var deployment = run.PilotRegistry.Assigned.Select(pilot => pilot.Id).Reverse().ToArray();
		run.PilotRegistry.RecordDeployment(deployment);
		var registry = PersistenceRegistry.CreateDefault();

		using var restored = State.FromSnapshot(run.CaptureSnapshot(registry), registry);

		Assert.Equal(deployment, restored.PilotRegistry.LastDeployment);
	}

	[Fact]
	public void FromSnapshot_RejectsIncompatiblePilotShipPairing()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var minerId = "restored-unclassified-ship";
		Assert.True(run.TryAcquireOwnedShip(
			new ShipSpawnDeclaration(minerId, BattleUnitType.RepurposedMiner)));
		var registry = PersistenceRegistry.CreateDefault();
		var pilot = run.PilotRegistry.CaptureSnapshot()
			.Single(snapshot => snapshot.ClassId == EOperatorClassId.SpaceRanger);
		var snapshot = run.CaptureSnapshot(registry) with
		{
			Pilots = [pilot with { AssignedShipId = minerId }],
		};

		var exception = Assert.Throws<InvalidDataException>(() =>
			State.FromSnapshot(snapshot, registry));

		Assert.Contains("cannot operate ship", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void TryDismissPilot_RemovesPilotAndKeepsOwnedShip()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var fighterShipId = RunTestKit.StarterFighterShipId(run);
		Assert.True(run.PilotRegistry.TryGetForShip(fighterShipId, out var pilot));
		var pilotId = pilot.Id;
		var pilotsBefore = run.PilotRegistry.All.Count;

		Assert.True(run.TryDismissPilot(pilotId));

		Assert.False(run.PilotRegistry.TryGet(pilotId, out _));
		Assert.Equal(pilotsBefore - 1, run.PilotRegistry.All.Count);
		Assert.True(run.ItemInventory.IsPlayerOwned(fighterShipId));
		Assert.DoesNotContain(fighterShipId, run.GetFieldedShipIds());
	}

	[Fact]
	public void TryDismissPilot_RejectsLastPilot()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var fighterShipId = RunTestKit.StarterFighterShipId(run);
		var minerShipId = RunTestKit.StarterRepurposedMinerShipId(run);
		Assert.True(run.PilotRegistry.TryGetForShip(fighterShipId, out var fighterPilot));
		Assert.True(run.PilotRegistry.TryGetForShip(minerShipId, out var minerPilot));
		Assert.True(run.TryDismissPilot(fighterPilot.Id));

		Assert.False(run.TryDismissPilot(minerPilot.Id));
		Assert.True(run.PilotRegistry.TryGet(minerPilot.Id, out _));
	}

	[Fact]
	public void Register_AllowsTenPilotsAndRejectsAnEleventh()
	{
		var registry = new RunPilotRegistry();
		for (var index = 0; index < 10; index++)
			registry.Register(Pilot.CreateForShip($"fighter-{index}"));

		Assert.Equal(10, registry.All.Count);
		Assert.Equal(10, registry.Assigned.Count);
		Assert.False(registry.CanRegister);
		Assert.Throws<InvalidOperationException>(() => registry.Register(Pilot.CreateForShip("eleventh")));
		registry.Register(registry.All.First());
		Assert.Equal(10, registry.All.Count);
	}

	[Fact]
	public void TryAcquirePilotHire_AddsReservesBeyondFourAndRoundTrips()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		RunTestKit.EnlistFourPilots(run);
		var fieldedBefore = run.GetFieldedShipIds().ToArray();

		for (var index = 0; index < 6; index++)
			Assert.True(run.TryAcquirePilotHire(new PilotHireCommitted(
				new PilotRecruitProfile($"Reserve {index}", "pilot_03", EPilotBackgroundId.PodRacer))));

		Assert.Equal(10, run.PilotRegistry.All.Count);
		Assert.False(run.TryAcquirePilotHire(new PilotHireCommitted(
			new PilotRecruitProfile("Eleventh", "pilot_03", EPilotBackgroundId.PodRacer))));
		Assert.Equal(4, run.PilotRegistry.Assigned.Count);
		Assert.Equal(fieldedBefore, run.GetFieldedShipIds());
		var registry = PersistenceRegistry.CreateDefault();
		using var restored = State.FromSnapshot(run.CaptureSnapshot(registry), registry);
		Assert.Equal(
			run.PilotRegistry.All.Select(pilot => (pilot.Id, pilot.Name, pilot.AssignedShipId)),
			restored.PilotRegistry.All.Select(pilot => (pilot.Id, pilot.Name, pilot.AssignedShipId)));
		Assert.Equal(fieldedBefore, restored.GetFieldedShipIds());
	}
}
