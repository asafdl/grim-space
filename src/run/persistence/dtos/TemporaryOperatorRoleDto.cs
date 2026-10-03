using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.Run.Persistence;

public sealed record TemporaryOperatorRoleDto(
	string FacilityId,
	string OperatorName,
	EFacilityOperatorRole Role,
	string SourceId);
