using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.World.StarSystem.Generation;

public sealed record StarSystemBlueprint(
	int Seed,
	int Width,
	int Height,
	EStarSystemClass SystemClass,
	SupplySystemPlan SupplyPlan,
	IReadOnlyList<PoiSpec> Pois,
	IReadOnlyList<UnitSpawnIntent> UnitSpawns);
