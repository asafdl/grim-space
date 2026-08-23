using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Generation;

public static class SupplySystemGenerator
{
	public const string MinerOneId = "unit-miner-1";
	public const string MinerTwoId = "unit-miner-2";
	public const string HaulerId = "unit-hauler-1";
	public const string FreighterId = "unit-freighter-1";

	public static StarSystemBlueprint CreateBlueprint(int seed)
	{
		var plan = SupplySystemPlan.Copper;

		var pois = new PoiSpec[]
		{
			new(
				SupplySystemPlan.StarPoiId,
				EPointOfInterestKind.Star,
				EPoiLogicalRole.Environment,
				DisplayName: "Star",
				Radius: 64),
			new(
				plan.ExtractionPoiId,
				EPointOfInterestKind.AsteroidField,
				EPoiLogicalRole.Extraction,
				DisplayName: "Copper Field",
				Radius: 32),
			new(
				plan.RefineryPoiId,
				EPointOfInterestKind.Planet,
				EPoiLogicalRole.Refinery,
				DisplayName: "Refinery",
				Radius: 32),
			new(
				plan.StoragePoiId,
				EPointOfInterestKind.Station,
				EPoiLogicalRole.Storage,
				DisplayName: "Storage",
				Radius: 32),
			new(
				plan.ExitPoiId,
				EPointOfInterestKind.Wormhole,
				EPoiLogicalRole.Exit,
				DisplayName: "Exit",
				Radius: 32),
		};

		var unitSpawns = new UnitSpawnIntent[]
		{
			new(
				MinerOneId,
				EType.MiningBarge,
				plan.RefineryPoiId,
				[plan.ExtractionPoiId, plan.RefineryPoiId]),
			new(
				MinerTwoId,
				EType.MiningBarge,
				plan.RefineryPoiId,
				[plan.ExtractionPoiId, plan.RefineryPoiId]),
			new(
				HaulerId,
				EType.RefineryHauler,
				plan.RefineryPoiId,
				[plan.StoragePoiId, plan.RefineryPoiId]),
			new(
				FreighterId,
				EType.ExportFreighter,
				plan.StoragePoiId,
				[plan.ExitPoiId, plan.StoragePoiId]),
		};

		return new StarSystemBlueprint(
			seed,
			StarMap.DevMapWidth,
			StarMap.DevMapHeight,
			EStarSystemClass.Supply,
			plan,
			pois,
			unitSpawns);
	}
}
