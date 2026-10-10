using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Landmarks;
using GrimSpace.World.StarSystem.Traffic;
using GrimSpace.World.StarSystem.Objectives;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Run.Persistence;

public sealed record StarMapSaveDto(
	StarMapBlueprintDto Blueprint,
	IReadOnlyList<StarMapPoiDto> Pois,
	IReadOnlyList<NavigationLandmark> Landmarks,
	IReadOnlyList<Dock> Docks,
	IReadOnlyList<SpaceRoute> Routes,
	IReadOnlyList<StarMapFleetDto> Fleets,
	IReadOnlyList<StarMapContractDto> Contracts,
	int MaxPendingContracts,
	IReadOnlyList<StoryObjective> StoryObjectives,
	IReadOnlyList<ResourceBalanceDto> Resources,
	bool WaitingForPlayerInput,
	string? ActiveNarrativeId,
	TimelineSnapshotDto Timeline);
