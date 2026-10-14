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
using GrimSpace.World.StarSystem.Skills;
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
	}

	[Fact]
	public void Enlist_AddsSecondPilotWhilePartyOrderStaysShipOrdered()
	{
		using var run = State.CreateNewRun(42);
		var firstShipId = run.GetFieldedShipIds()[0];
		var secondShipId = TypedIdGenerator.NextId("gunship");
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(secondShipId, BattleUnitType.Gunship)));

		Assert.Equal([firstShipId, secondShipId], run.GetFieldedShipIds());
		Assert.True(run.PilotRegistry.TryGetForShip(firstShipId, out var firstPilot));
		Assert.True(run.PilotRegistry.TryGetForShip(secondShipId, out var secondPilot));
		Assert.NotEqual(firstPilot.Id, secondPilot.Id);
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
				.AddExperience(Pilot.CreateForShip($"fighter-{index}"), 100)
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
		var secondShipId = TypedIdGenerator.NextId("gunship");
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(secondShipId, BattleUnitType.Gunship)));
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
		Assert.Equal(115, scrap);
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
	}

	[Fact]
	public void RegenerateMap_PreservesPartyAndPilots()
	{
		using var run = State.CreateNewRun(42);
		var secondShipId = TypedIdGenerator.NextId("gunship");
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(secondShipId, BattleUnitType.Gunship)));
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
	public void FromSnapshot_ThrowsWhenPartyShipHasNoPilot()
	{
		using var run = State.CreateNewRun(42);
		var registry = PersistenceRegistry.CreateDefault();
		var snapshot = run.CaptureSnapshot(registry) with
		{
			Pilots = [],
			PartyShipIds = run.GetFieldedShipIds(),
		};

		Assert.Throws<InvalidDataException>(() => State.FromSnapshot(snapshot, registry));
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
