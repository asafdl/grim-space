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
	public void CreateNewRun_RegistersPilotForStarterShip()
	{
		using var run = State.CreateNewRun(42);
		Assert.Equal(2, run.GetFieldedShipIds().Count);
		var shipId = RunTestKit.StarterFighterShipId(run);

		Assert.True(run.PilotRegistry.TryGetForShip(shipId, out var pilot));
		Assert.StartsWith("pilot-", pilot.Id, StringComparison.Ordinal);
		Assert.False(string.IsNullOrWhiteSpace(pilot.Name));
		Assert.Contains(pilot.PortraitId, Pilot.PortraitPool);
		Assert.Equal(shipId, pilot.AssignedShipId);
		Assert.True(pilot.IsActive);
		Assert.Equal(2, run.PilotRegistry.Active.Count);
		Assert.Equal(pilot, RunTestKit.StarterRangerPilot(run));
		Assert.Equal(PilotProgression.LevelFor(0), pilot.ClaimedLevel);
		Assert.Empty(pilot.LearnedSkills);
		Assert.Equal(EPilotBackgroundId.ScrapDrone, pilot.BackgroundId);
		Assert.Equal(EPilotQuirkId.Charismatic, pilot.PositiveQuirkId);
		Assert.Equal(EPilotQuirkId.NeuroDrugAddict, pilot.NegativeQuirkId);
	}

	[Fact]
	public void CreateNewRun_FieldedRosterIncludesSupportEngineerOnRepurposedMinerT0()
	{
		using var run = State.CreateNewRun(42);
		var minerShipId = RunTestKit.StarterRepurposedMinerShipId(run);
		var ship = run.ItemInventory.Get(minerShipId);

		Assert.Equal(BattleUnitType.RepurposedMiner, ship.Spec.Chassis);
		Assert.Equal(
			ShipCatalog.LoadoutForTier(BattleUnitType.RepurposedMiner, EShipGearTier.T0).MaxHullPoints,
			ship.Loadout.MaxHullPoints);
		Assert.True(run.PilotRegistry.TryGetForShip(minerShipId, out var pilot));
		Assert.Equal(EOperatorClassId.Support, pilot.ClassId);
		Assert.Equal(minerShipId, pilot.AssignedShipId);
		Assert.True(pilot.IsActive);
		Assert.Equal(2, run.PilotRegistry.Active.Count);
	}

	[Fact]
	public void Enlist_AddsSecondPilotWhilePartyOrderStaysShipOrdered()
	{
		using var run = State.CreateNewRun(42);
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
		using var run = State.CreateNewRun(42);
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
		using var run = State.CreateNewRun(42);
		var shipId = RunTestKit.StarterFighterShipId(run);
		Assert.True(run.PilotRegistry.TryGetForShip(shipId, out var originalPilot));
		var replacement = Pilot.CreateForShip("reserve-ship");
		run.PilotRegistry.Register(replacement);
		run.PilotRegistry.Unassign(replacement.Id);

		Assert.True(run.TryAssignPilotToShip(replacement.Id, shipId));

		Assert.Equal(replacement.Id, run.PilotRegistry.Active.Single(active => active.Id == replacement.Id).Id);
		Assert.Equal(shipId, RunTestKit.StarterFighterShipId(run));
		Assert.True(run.PilotRegistry.TryGet(replacement.Id, out var assignedReplacement));
		Assert.Equal(shipId, assignedReplacement.AssignedShipId);
		Assert.True(assignedReplacement.IsActive);
		Assert.True(run.PilotRegistry.TryGet(originalPilot.Id, out var replacedPilot));
		Assert.Null(replacedPilot.AssignedShipId);
		Assert.False(replacedPilot.IsActive);
	}

	[Fact]
	public void TryAssignPilotToShip_KeepsActivePilotWhenMovingToEmptyOwnedShip()
	{
		using var run = State.CreateNewRun(42);
		var starterShipId = RunTestKit.StarterFighterShipId(run);
		Assert.True(run.PilotRegistry.TryGetForShip(starterShipId, out var pilot));
		var emptyShipId = TypedIdGenerator.NextId("fighter");
		Assert.True(run.TryAcquireOwnedShip(
			new ShipSpawnDeclaration(emptyShipId, BattleUnitType.Fighter, EShipGearTier.T1)));
		var activeBefore = run.PilotRegistry.Active.Count;

		Assert.True(run.TryAssignPilotToShip(pilot.Id, emptyShipId));

		Assert.True(run.PilotRegistry.TryGet(pilot.Id, out var moved));
		Assert.Equal(emptyShipId, moved.AssignedShipId);
		Assert.True(moved.IsActive);
		Assert.Equal(activeBefore, run.PilotRegistry.Active.Count);
		Assert.Contains(emptyShipId, run.GetFieldedShipIds());
		Assert.DoesNotContain(starterShipId, run.GetFieldedShipIds());
		Assert.False(run.PilotRegistry.TryGetForShip(starterShipId, out _));
	}

	[Fact]
	public void TryAssignPilotToShip_FieldsReservePilotOntoEmptyShipWhenSquadHasRoom()
	{
		using var run = State.CreateNewRun(42);
		Assert.Equal(2, run.PilotRegistry.Active.Count);
		var emptyShipId = TypedIdGenerator.NextId("fighter");
		Assert.True(run.TryAcquireOwnedShip(
			new ShipSpawnDeclaration(emptyShipId, BattleUnitType.Fighter, EShipGearTier.T1)));
		var reserve = Pilot.CreateForShip("reserve-for-empty");
		run.PilotRegistry.Register(reserve);
		run.PilotRegistry.Unassign(reserve.Id);

		Assert.True(run.TryAssignPilotToShip(reserve.Id, emptyShipId));

		Assert.True(run.PilotRegistry.TryGet(reserve.Id, out var fielded));
		Assert.Equal(emptyShipId, fielded.AssignedShipId);
		Assert.True(fielded.IsActive);
		Assert.Equal(3, run.PilotRegistry.Active.Count);
		Assert.Contains(emptyShipId, run.GetFieldedShipIds());
	}

	[Fact]
	public void EnsureActiveRosterFilledFromAssignments_ActivatesAssignedInactivePilots()
	{
		using var run = State.CreateNewRun(42);
		var emptyShipId = TypedIdGenerator.NextId("fighter");
		Assert.True(run.TryAcquireOwnedShip(
			new ShipSpawnDeclaration(emptyShipId, BattleUnitType.Fighter, EShipGearTier.T1)));
		var reserve = Pilot.CreateForShip(emptyShipId);
		run.PilotRegistry.Register(reserve);
		Assert.False(reserve.IsActive);
		Assert.Equal(emptyShipId, reserve.AssignedShipId);

		Assert.True(run.EnsureActiveRosterFilledFromAssignments());

		Assert.True(run.PilotRegistry.TryGet(reserve.Id, out var fielded));
		Assert.True(fielded.IsActive);
		Assert.Contains(emptyShipId, run.GetFieldedShipIds());
	}

	[Fact]
	public void TryAssignPilotToShip_RejectsUnknownPilotOrUnownedShip()
	{
		using var run = State.CreateNewRun(42);
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
		using var run = State.CreateNewRun(42);
		var pilot = RunTestKit.StarterRangerPilot(run);
		var shipId = "unclassified-reserve";
		Assert.True(run.TryAcquireOwnedShip(new ShipSpawnDeclaration(shipId, BattleUnitType.Gunship)));
		var fieldedBefore = run.GetFieldedShipIds().ToArray();
		var activeBefore = run.PilotRegistry.Active.ToArray();

		Assert.False(run.TryAssignPilotToShip(
			pilot.Id,
			shipId,
			out var failureReason));

		Assert.Equal(EPilotAssignmentFailureReason.IncompatibleClass, failureReason);
		Assert.Equal(fieldedBefore, run.GetFieldedShipIds());
		Assert.Equal(activeBefore, run.PilotRegistry.Active);
		Assert.Equal(run.ItemInventory.Get(shipId).Id, shipId);
		Assert.Equal(run.ItemInventory.Get(fieldedBefore[0]).Id, pilot.AssignedShipId);
	}

	[Fact]
	public void TryAssignPilotToShip_RejectsSwapWhenEitherResultingPairingIsIncompatible()
	{
		using var run = State.CreateNewRun(42);
		var fighterShipId = RunTestKit.StarterFighterShipId(run);
		var minerShipId = "unclassified-swap";
		Assert.True(run.TryAcquireOwnedShip(new ShipSpawnDeclaration(minerShipId, BattleUnitType.RepurposedMiner)));
		var pilot = RunTestKit.StarterRangerPilot(run);
		var reservePilot = Pilot.CreateForShip("reserve-id");
		run.PilotRegistry.Register(reservePilot);
		run.PilotRegistry.Unassign(reservePilot.Id);
		var assignmentsBefore = run.PilotRegistry.All
			.OrderBy(candidate => candidate.Id, StringComparer.Ordinal)
			.Select(candidate => (candidate.Id, candidate.AssignedShipId, candidate.IsActive))
			.ToArray();

		Assert.False(run.TryAssignPilotToShip(pilot.Id, minerShipId));
		Assert.Equal(
			assignmentsBefore,
			run.PilotRegistry.All
				.OrderBy(candidate => candidate.Id, StringComparer.Ordinal)
				.Select(candidate => (candidate.Id, candidate.AssignedShipId, candidate.IsActive))
				.ToArray());
		Assert.Equal(fighterShipId, pilot.AssignedShipId);
	}

	[Fact]
	public void RegistryDirectAssignmentAndActivationRejectIncompatibleShip()
	{
		var registry = new RunPilotRegistry();
		var inventory = new GrimSpace.Run.Items.Inventory();
		const string playerFleet = State.PlayerFleetUnitId;
		var pilot = Pilot.CreateForShip("unclassified-ship");
		inventory.Register(
			ShipInstance.FromCatalog("unclassified-ship", EType.RepurposedMiner),
			playerFleet);
		registry.Register(pilot);

		Assert.Throws<InvalidOperationException>(() =>
			registry.Activate(pilot.Id, inventory, playerFleet));
		Assert.Throws<InvalidOperationException>(() =>
			registry.AssignShip(pilot.Id, "unclassified-ship", inventory, playerFleet));
		Assert.Empty(registry.Active);
		Assert.True(registry.TryGet(pilot.Id, out var unchanged));
		Assert.False(unchanged.IsActive);
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
			.Select(candidate => (candidate.Id, candidate.AssignedShipId, candidate.IsActive))
			.ToArray();

		Assert.Throws<InvalidOperationException>(() =>
			registry.SwapAssignments(fighterPilot.Id, minerPilot.Id, inventory, playerFleet));
		Assert.Equal(
			before,
			registry.All
				.OrderBy(candidate => candidate.Id, StringComparer.Ordinal)
				.Select(candidate => (candidate.Id, candidate.AssignedShipId, candidate.IsActive))
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
		using var run = State.CreateNewRun(42);
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
		using var run = State.CreateNewRun(42);
		var pilot = RunTestKit.StarterRangerPilot(run);
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
		Assert.Equal(pilot.PositiveQuirkId, restored.PositiveQuirkId);
		Assert.Equal(pilot.NegativeQuirkId, restored.NegativeQuirkId);
	}

	[Fact]
	public void RegenerateMap_PreservesPartyAndPilots()
	{
		using var run = State.CreateNewRun(42);
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
	public void FromSnapshot_ThrowsWhenActivePilotHasNoAssignedShip()
	{
		using var run = State.CreateNewRun(42);
		var registry = PersistenceRegistry.CreateDefault();
		var snapshot = run.CaptureSnapshot(registry) with
		{
			Pilots =
			[
				run.PilotRegistry.CaptureSnapshot()[0] with
				{
					IsActive = true,
					ActiveOrder = 0,
					AssignedShipId = null,
				},
			],
		};

		Assert.Throws<InvalidDataException>(() => State.FromSnapshot(snapshot, registry));
	}

	[Fact]
	public void FromSnapshot_RejectsIncompatiblePilotShipPairing()
	{
		using var run = State.CreateNewRun(42);
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
		using var run = State.CreateNewRun(42);
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
		using var run = State.CreateNewRun(42);
		var fighterShipId = RunTestKit.StarterFighterShipId(run);
		var minerShipId = RunTestKit.StarterRepurposedMinerShipId(run);
		Assert.True(run.PilotRegistry.TryGetForShip(fighterShipId, out var fighterPilot));
		Assert.True(run.PilotRegistry.TryGetForShip(minerShipId, out var minerPilot));
		Assert.True(run.TryDismissPilot(fighterPilot.Id));

		Assert.False(run.TryDismissPilot(minerPilot.Id));
		Assert.True(run.PilotRegistry.TryGet(minerPilot.Id, out _));
	}

	[Fact]
	public void Register_RespectsMaxPilotCap()
	{
		var registry = new RunPilotRegistry();
		for (var index = 0; index < RunPilotRegistry.MaxPilots; index++)
			registry.Register(Pilot.CreateForShip($"fighter-{index}"));

		Assert.False(registry.CanRegister);
		Assert.Throws<InvalidOperationException>(() =>
			registry.Register(Pilot.CreateForShip("fighter-overflow")));
	}

	[Fact]
	public void TryAcquirePilotHire_RejectsWhenRosterFull()
	{
		using var run = State.CreateNewRun(42);
		RunTestKit.EnlistUntilActivePilotCap(run);
		Assert.Equal(RunPilotRegistry.MaxPilots, run.PilotRegistry.All.Count);

		Assert.False(run.TryAcquirePilotHire(new PilotHireCommitted(
			new PilotRecruitProfile("Overflow", "pilot_03", EPilotBackgroundId.PodRacer))));
	}
}
