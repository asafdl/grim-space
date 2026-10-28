using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Ids;
using GrimSpace.Run;
using GrimSpace.Run.Persistence;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.Pilots;
using GrimSpace.World.StarSystem.Effects;
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
		var shipId = Assert.Single(run.GetFieldedShipIds());

		Assert.True(run.PilotRegistry.TryGetForShip(shipId, out var pilot));
		Assert.StartsWith("pilot-", pilot.Id, StringComparison.Ordinal);
		Assert.False(string.IsNullOrWhiteSpace(pilot.Name));
		Assert.Contains(pilot.PortraitId, Pilot.PortraitPool);
		Assert.Equal(shipId, pilot.AssignedShipId);
		Assert.True(pilot.IsActive);
		Assert.Equal(pilot, Assert.Single(run.PilotRegistry.Active));
		Assert.Equal(PilotProgression.LevelFor(0), pilot.ClaimedLevel);
		Assert.Empty(pilot.LearnedSkills);
		Assert.Equal(EPilotBackgroundId.ScrapDrone, pilot.BackgroundId);
		Assert.Equal(EPilotQuirkId.Charismatic, pilot.PositiveQuirkId);
		Assert.Equal(EPilotQuirkId.NeuroDrugAddict, pilot.NegativeQuirkId);
	}

	[Fact]
	public void Enlist_AddsSecondPilotWhilePartyOrderStaysShipOrdered()
	{
		using var run = State.CreateNewRun(42);
		var firstShipId = run.GetFieldedShipIds()[0];
		var secondShipId = TypedIdGenerator.NextId("fighter");
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(secondShipId, BattleUnitType.Fighter)));

		Assert.Equal([firstShipId, secondShipId], run.GetFieldedShipIds());
		Assert.True(run.PilotRegistry.TryGetForShip(firstShipId, out var firstPilot));
		Assert.True(run.PilotRegistry.TryGetForShip(secondShipId, out var secondPilot));
		Assert.NotEqual(firstPilot.Id, secondPilot.Id);
	}

	[Fact]
	public void TryAssignPilotToShip_SwapsActivePilotsWhilePreservingFieldedShipOrder()
	{
		using var run = State.CreateNewRun(42);
		var firstShipId = run.GetFieldedShipIds()[0];
		var secondShipId = TypedIdGenerator.NextId("fighter");
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(secondShipId, BattleUnitType.Fighter)));
		Assert.True(run.PilotRegistry.TryGetForShip(firstShipId, out var firstPilot));
		Assert.True(run.PilotRegistry.TryGetForShip(secondShipId, out var secondPilot));

		Assert.True(run.TryAssignPilotToShip(secondPilot.Id, firstShipId));

		Assert.Equal([firstShipId, secondShipId], run.GetFieldedShipIds());
		Assert.True(run.PilotRegistry.TryGetForShip(firstShipId, out var reassignedFirst));
		Assert.True(run.PilotRegistry.TryGetForShip(secondShipId, out var reassignedSecond));
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
		var shipId = Assert.Single(run.GetFieldedShipIds());
		Assert.True(run.PilotRegistry.TryGetForShip(shipId, out var originalPilot));
		var replacement = Pilot.CreateForShip("reserve-ship");
		run.PilotRegistry.Register(replacement);
		run.PilotRegistry.Unassign(replacement.Id);

		Assert.True(run.TryAssignPilotToShip(replacement.Id, shipId));

		Assert.Equal(replacement.Id, Assert.Single(run.PilotRegistry.Active).Id);
		Assert.Equal(shipId, Assert.Single(run.GetFieldedShipIds()));
		Assert.True(run.PilotRegistry.TryGet(replacement.Id, out var assignedReplacement));
		Assert.Equal(shipId, assignedReplacement.AssignedShipId);
		Assert.True(assignedReplacement.IsActive);
		Assert.True(run.PilotRegistry.TryGet(originalPilot.Id, out var replacedPilot));
		Assert.Null(replacedPilot.AssignedShipId);
		Assert.False(replacedPilot.IsActive);
	}

	[Fact]
	public void TryAssignPilotToShip_RejectsUnknownPilotOrUnownedShip()
	{
		using var run = State.CreateNewRun(42);
		var pilot = Assert.Single(run.PilotRegistry.Active);

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
		var pilot = Assert.Single(run.PilotRegistry.Active);
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
		var fighterShipId = run.GetFieldedShipIds()[0];
		var minerShipId = "unclassified-swap";
		Assert.True(run.TryAcquireOwnedShip(new ShipSpawnDeclaration(minerShipId, BattleUnitType.RepurposedMiner)));
		var pilot = Assert.Single(run.PilotRegistry.Active);
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
		var pilot = Assert.Single(run.PilotRegistry.All);
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
		var pilot = Assert.Single(run.PilotRegistry.CaptureSnapshot());
		var snapshot = run.CaptureSnapshot(registry) with
		{
			Pilots = [pilot with { AssignedShipId = minerId }],
		};

		var exception = Assert.Throws<InvalidDataException>(() =>
			State.FromSnapshot(snapshot, registry));

		Assert.Contains("cannot operate ship", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Activate_RespectsMaxActivePilotCap()
	{
		var registry = new RunPilotRegistry();
		var inventory = new GrimSpace.Run.Items.Inventory();
		const string playerFleet = State.PlayerFleetUnitId;
		var pilots = new List<Pilot>();
		for (var index = 0; index < RunPilotRegistry.MaxActivePilots + 1; index++)
		{
			var shipId = $"fighter-{index}";
			inventory.Register(ShipInstance.FromCatalog(shipId, EType.Fighter), playerFleet);
			var pilot = Pilot.CreateForShip(shipId);
			registry.Register(pilot);
			pilots.Add(pilot);
		}

		for (var index = 0; index < RunPilotRegistry.MaxActivePilots; index++)
			registry.Activate(pilots[index].Id, inventory, playerFleet);

		Assert.Throws<InvalidOperationException>(() =>
			registry.Activate(pilots[^1].Id, inventory, playerFleet));
	}
}
