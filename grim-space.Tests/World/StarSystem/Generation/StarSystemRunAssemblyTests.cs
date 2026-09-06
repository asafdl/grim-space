using GrimSpace.Run;
using GrimSpace.Tests.World.StarSystem.Traffic;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Units;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.World.StarSystem.Generation;

public sealed class StarSystemRunAssemblyTests
{
	[Fact]
	public void CreateDevSession_AddsPlayerFleetAtTradeHub()
	{
		var starSystem = StarSystemOrchestrator.CreateDevSession(RunState.PlayerFleetUnitId, 42);

		Assert.Equal(27, starSystem.Map.UnitRegistry.Ids.Count());
		var playerFleet = starSystem.Map.UnitRegistry.UnitOf(RunState.PlayerFleetUnitId);
		Assert.Equal(EType.PlayerFleet, playerFleet.State.Type);
		Assert.Empty(playerFleet.State.ChoreDockIds);
		Assert.Equal(
			starSystem.Map.DocksByPoiId[SupplySystemPlan.Copper.TradeHubPoiId].Id,
			playerFleet.State.DockedAtDockId);
	}

	[Fact]
	public void CreateDevDefault_HasTwentySixNpcUnitsOnly()
	{
		var map = StarMap.CreateDevDefault(42);

		Assert.Equal(26, map.UnitRegistry.Ids.Count());
		Assert.DoesNotContain(map.UnitRegistry.Ids, id => id == RunState.PlayerFleetUnitId);
	}

	[Fact]
	public void CreateDevSession_RespawnsPlayerFleetAtTradeHub()
	{
		var first = StarSystemOrchestrator.CreateDevSession(RunState.PlayerFleetUnitId, 7);
		var second = StarSystemOrchestrator.CreateDevSession(RunState.PlayerFleetUnitId, 7);

		var firstFleet = first.Map.UnitRegistry.UnitOf(RunState.PlayerFleetUnitId);
		var secondFleet = second.Map.UnitRegistry.UnitOf(RunState.PlayerFleetUnitId);
		Assert.Equal(firstFleet.State.DockedAtDockId, secondFleet.State.DockedAtDockId);
		Assert.Equal(EPhase.Docked, secondFleet.State.Phase);
	}

	[Fact]
	public void AddPlayerFleet_AddsPlayerFleetAtTradeHub()
	{
		var map = StarMap.CreateDevDefault(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);

		Assert.Equal(27, map.UnitRegistry.Ids.Count());
		var playerFleet = map.UnitRegistry.UnitOf(RunState.PlayerFleetUnitId);
		Assert.Equal(
			map.DocksByPoiId[SupplySystemPlan.Copper.TradeHubPoiId].Id,
			playerFleet.State.DockedAtDockId);
	}
}
