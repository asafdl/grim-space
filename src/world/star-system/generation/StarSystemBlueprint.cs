using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.World.StarSystem.Generation;

public sealed record StarSystemBlueprint(
	int Seed,
	int Width,
	int Height,
	EStarSystemClass SystemClass,
	EFaction ControllingFaction,
	SupplySystemPlan SupplyPlan,
	IReadOnlyList<PointOfInterest> PoiTemplates,
	IReadOnlyList<UnitSpawnIntent> UnitSpawns);
