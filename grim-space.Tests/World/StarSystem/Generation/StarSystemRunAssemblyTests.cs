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
	public void Assemble_AddsPlayerFleetAtTradeHub()
	{
		var buildResult = StarMap.CreateDevBuildResult(42);
		StarSystemTestHarness.AddPlayerFleet(buildResult, RunState.PlayerFleetUnitId);

		Assert.Equal(27, buildResult.Map.UnitRegistry.Ids.Count());
		var playerFleet = buildResult.Map.UnitRegistry.UnitOf(RunState.PlayerFleetUnitId);
		Assert.Equal(EType.PlayerFleet, playerFleet.State.Type);
		Assert.Empty(playerFleet.State.ChoreDockIds);
		Assert.Equal(
			buildResult.Map.DocksByPoiId[SupplySystemPlan.Copper.TradeHubPoiId].Id,
			playerFleet.State.DockedAtDockId);
	}

	[Fact]
	public void CreateDevBuildResult_HasTwentySixNpcUnitsOnly()
	{
		var buildResult = StarMap.CreateDevBuildResult(42);

		Assert.Equal(26, buildResult.Map.UnitRegistry.Ids.Count());
		Assert.DoesNotContain(
			buildResult.Map.UnitRegistry.Ids,
			id => id == RunState.PlayerFleetUnitId);
	}

	[Fact]
	public void Assemble_RespawnsPlayerFleetAtTradeHub()
	{
		var firstBuild = StarMap.CreateDevBuildResult(7);
		StarSystemTestHarness.AddPlayerFleet(firstBuild, RunState.PlayerFleetUnitId);
		var secondBuild = StarMap.CreateDevBuildResult(7);
		StarSystemTestHarness.AddPlayerFleet(secondBuild, RunState.PlayerFleetUnitId);

		var firstFleet = firstBuild.Map.UnitRegistry.UnitOf(RunState.PlayerFleetUnitId);
		var secondFleet = secondBuild.Map.UnitRegistry.UnitOf(RunState.PlayerFleetUnitId);
		Assert.Equal(firstFleet.State.DockedAtDockId, secondFleet.State.DockedAtDockId);
		Assert.Equal(EPhase.Docked, secondFleet.State.Phase);
	}
}
