using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.Tests.World.StarSystem.Poi;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

internal static class ContractActionTestContext
{
	public static string AdministrativePoiId => SupplySystemPlan.Copper.AdministrativePoiId;

	public static string ManagementFacilityId =>
		Facility.ScopedId(AdministrativePoiId, AdministrativeCore.ManagementFacilitySlug);

	public static AcceptContractAction Accept(StarMap map, string actorId, string contractId) =>
		new(
			actorId,
			AdministrativePoiId,
			ManagementFacilityId,
			MapFacilityOperators.ContractOperatorName(map),
			contractId);

	public static DeclineContractAction Decline(StarMap map, string actorId, string contractId) =>
		new(
			actorId,
			AdministrativePoiId,
			ManagementFacilityId,
			MapFacilityOperators.ContractOperatorName(map),
			contractId);
}
