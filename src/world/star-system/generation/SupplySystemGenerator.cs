using GrimSpace.Core.Ids;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Generation;

public static class SupplySystemGenerator
{
	public static StarSystemBlueprint CreateBlueprint(int seed)
	{
		var plan = SupplySystemPlan.Copper;

		var minerChore = new[] { plan.ExtractionPoiId, plan.RefineryPoiId };
		var haulerChore = new[] { plan.StoragePoiId, plan.RefineryPoiId };
		var freighterChore = new[] { plan.ExitPoiId, plan.StoragePoiId };
		var unitSpawns = new[]
		{
			SpawnUnits(EType.MiningBarge, 4, plan.RefineryPoiId, minerChore),
			SpawnUnits(EType.RefineryHauler, 2, plan.RefineryPoiId, haulerChore),
			SpawnUnits(EType.ExportFreighter, 2, plan.StoragePoiId, freighterChore),
		}.SelectMany(spawns => spawns).ToArray();

		return new StarSystemBlueprint(
			seed,
			StarMap.DevMapWidth,
			StarMap.DevMapHeight,
			EStarSystemClass.Supply,
			plan,
			plan.CreatePoiTemplates(),
			unitSpawns);
	}

	private static IEnumerable<UnitSpawnIntent> SpawnUnits(
		EType type,
		int count,
		string startPoiId,
		IReadOnlyList<string> chorePoiIds)
	{
		var typeSlug = StarSystemTypeSlug.For(type);
		for (var i = 0; i < count; i++)
			yield return new UnitSpawnIntent(TypedIdGenerator.NextId(typeSlug), type, startPoiId, chorePoiIds);
	}
}
