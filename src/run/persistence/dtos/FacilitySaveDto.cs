using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.Run.Persistence;

public sealed record FacilitySaveDto(
	string Id,
	string DisplayName,
	EPresentationAnchor PresentationAnchor,
	string ScenePath,
	IReadOnlyList<FacilityOperator> Operators);
