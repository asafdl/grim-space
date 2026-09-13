using GrimSpace.Run;
using GrimSpace.Tests.World.StarSystem.Traffic;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Units;
using RunState = GrimSpace.Run.State;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Generation;

public sealed class StarSystemRunAssemblyTests(DevStarMapFixture maps)
{
	[Fact]
	public void CreateDevSession_AddsPlayerFleetAtTradeHub()
	{
		var starSystem = StarSystemOrchestrator.CreateDevSession(RunState.PlayerFleetUnitId, 42);

		Assert.Equal(27, starSystem.Map.FleetRegistry.Ids.Count());
		var playerFleet = starSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		Assert.Equal(EType.PlayerFleet, playerFleet.State.Type);
		var member = Assert.Single(playerFleet.Members);
		Assert.Equal(GrimSpace.Units.Enums.EType.Fighter, member.Type);
		Assert.StartsWith("fighter-", member.Id);
		Assert.Empty(playerFleet.State.ChoreDockIds);
		Assert.Equal(
			starSystem.Map.DocksByPoiId[SupplySystemPlan.Copper.TradeHubPoiId].Id,
			playerFleet.State.DockedAtDockId);
	}

	[Fact]
	public void CreateDevDefault_PlayerPartyAndWorldFleetShareMembers()
	{
		var run = RunState.CreateDevDefault(42);
		var worldFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);

		Assert.Equal(run.PlayerParty.Members, worldFleet.Members);
		Assert.Same(run.PlayerParty.Members[0], worldFleet.Members[0]);
	}

	[Fact]
	public void CreateDevDefault_HasTwentySixNpcUnitsOnly()
	{
		var map = maps.Fresh(42);

		Assert.Equal(26, map.FleetRegistry.Ids.Count());
		Assert.DoesNotContain(map.FleetRegistry.Ids, id => id == RunState.PlayerFleetUnitId);
		Assert.All(map.FleetRegistry.All, fleet => Assert.Empty(fleet.Members));
	}

	[Fact]
	public void CreateDevSession_RespawnsPlayerFleetAtTradeHub()
	{
		var first = StarSystemOrchestrator.CreateDevSession(RunState.PlayerFleetUnitId, 7);
		var second = StarSystemOrchestrator.CreateDevSession(RunState.PlayerFleetUnitId, 7);

		var firstFleet = first.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var secondFleet = second.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		Assert.Equal(firstFleet.State.DockedAtDockId, secondFleet.State.DockedAtDockId);
		Assert.Equal(EPhase.Docked, secondFleet.State.Phase);
	}

	[Fact]
	public void Subscribe_ExposesCommittedEntries()
	{
		using var starSystem = StarSystemOrchestrator.CreateDevSession(
			RunState.PlayerFleetUnitId,
			42);
		BeginNarrativeAction? received = null;
		using var subscription = starSystem.Subscribe<BeginNarrativeAction>(
			action => received = action);
		var action = new BeginNarrativeAction(RunState.PlayerFleetUnitId, "test-narrative");

		starSystem.CommitSetup(action);

		Assert.Equal(action, received);
	}

	[Fact]
	public void AddPlayerFleet_AddsPlayerFleetAtTradeHub()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);

		Assert.Equal(27, map.FleetRegistry.Ids.Count());
		var playerFleet = map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		Assert.Equal(
			map.DocksByPoiId[SupplySystemPlan.Copper.TradeHubPoiId].Id,
			playerFleet.State.DockedAtDockId);
	}
}
