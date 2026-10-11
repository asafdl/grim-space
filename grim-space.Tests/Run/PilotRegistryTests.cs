using GrimSpace.Core.Ids;
using GrimSpace.Run;
using GrimSpace.Run.Persistence;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Actions;
using BattleUnitType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class PilotRegistryTests
{
	[Fact]
	public void CreateNewRun_RegistersPilotForStarterShip()
	{
		using var run = State.CreateNewRun(42);
		var shipId = Assert.Single(run.PlayerParty.ShipIds);

		Assert.True(run.PilotRegistry.TryGetForShip(shipId, out var pilot));
		Assert.StartsWith("pilot-", pilot.Id, StringComparison.Ordinal);
		Assert.False(string.IsNullOrWhiteSpace(pilot.Name));
		Assert.Contains(pilot.PortraitId, Pilot.PortraitPool);
		Assert.Equal(shipId, pilot.AssignedShipId);
	}

	[Fact]
	public void Enlist_AddsSecondPilotWhilePartyOrderStaysShipOrdered()
	{
		using var run = State.CreateNewRun(42);
		var firstShipId = run.PlayerParty.ShipIds[0];
		var secondShipId = TypedIdGenerator.NextId("gunship");
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(secondShipId, BattleUnitType.Gunship)));

		Assert.Equal([firstShipId, secondShipId], run.PlayerParty.ShipIds);
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

		Assert.Equal(run.PlayerParty.ShipIds, restored.PlayerParty.ShipIds);
		foreach (var shipId in run.PlayerParty.ShipIds)
		{
			Assert.True(run.PilotRegistry.TryGetForShip(shipId, out var original));
			Assert.True(restored.PilotRegistry.TryGetForShip(shipId, out var roundTripped));
			Assert.Equal(original.Id, roundTripped.Id);
			Assert.Equal(original.Name, roundTripped.Name);
			Assert.Equal(original.PortraitId, roundTripped.PortraitId);
			Assert.Equal(original.AssignedShipId, roundTripped.AssignedShipId);
			Assert.Equal(original.ExperiencePoints, roundTripped.ExperiencePoints);
		}
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
		var pilotsBefore = run.PlayerParty.ShipIds
			.Select(shipId =>
			{
				Assert.True(run.PilotRegistry.TryGetForShip(shipId, out var pilot));
				return pilot;
			})
			.ToArray();

		run.RegenerateMap(77);

		Assert.Equal(pilotsBefore.Length, run.PlayerParty.ShipIds.Count);
		foreach (var pilot in pilotsBefore)
		{
			Assert.Contains(pilot.AssignedShipId, run.PlayerParty.ShipIds);
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
		var snapshot = run.CaptureSnapshot(registry) with { Pilots = [] };

		Assert.Throws<InvalidDataException>(() => State.FromSnapshot(snapshot, registry));
	}
}
