using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Generation;

public static class StarSystemRunAssembly
{
	public static (StarMap Map, StarSystemOrchestrator Traffic) Assemble(string playerFleetUnitId, int seed = 0)
	{
		ArgumentException.ThrowIfNullOrEmpty(playerFleetUnitId);
		var buildResult = StarMap.CreateDevBuildResult(seed);
		AddPlayerFleet(buildResult, playerFleetUnitId);
		var traffic = StarSystemOrchestrator.FromBuildResult(buildResult, playerFleetUnitId);
		return (buildResult.Map, traffic);
	}

	private static void AddPlayerFleet(StarSystemBuildResult buildResult, string playerFleetUnitId)
	{
		var map = buildResult.Map;
		var tradeHubDock = map.DocksByPoiId[SupplySystemPlan.Copper.TradeHubPoiId];
		var playerFleet = Factory.Create(new Spawn(
			playerFleetUnitId,
			EType.PlayerFleet,
			tradeHubDock.Id,
			UnitDefaults.SpeedPerTick(EType.PlayerFleet),
			[]));
		map.UnitRegistry.Add(playerFleet);
	}
}
