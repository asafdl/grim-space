using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Generation;

namespace GrimSpace.Run.Persistence;

public sealed record StarMapBlueprintDto(
	int Seed,
	int Width,
	int Height,
	EStarSystemClass SystemClass,
	EFaction ControllingFaction,
	SupplySystemPlan SupplyPlan);
