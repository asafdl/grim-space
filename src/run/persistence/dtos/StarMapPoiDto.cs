using GrimSpace.World.StarSystem.Poi;
using GrimSpace.Math.Grid;

namespace GrimSpace.Run.Persistence;

public sealed record StarMapPoiDto(
	string Id,
	string Kind,
	string DisplayName,
	int Radius,
	EPoiLogicalRole LogicalRole,
	EPoiPhysicalForm? PhysicalForm,
	Coord? Center,
	PoiFacade Facade,
	IReadOnlyList<FacilitySaveDto> Facilities,
	int NextAvailableTaskTick,
	IReadOnlyList<TemporaryOperatorRoleDto> TemporaryRoles);
