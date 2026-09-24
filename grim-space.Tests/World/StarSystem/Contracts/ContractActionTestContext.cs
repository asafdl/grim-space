using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.World.StarSystem.Runtime;
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

	public static AcceptContractAction AcceptDelivery(StarMap map, string actorId, string contractId)
	{
		var poiId = map.Blueprint.SupplyPlan.StoragePoiId;
		return new AcceptContractAction(
			actorId,
			poiId,
			Facility.ScopedId(poiId, StorageFacility.WarehouseFacilitySlug),
			MapFacilityOperators.WarehouseManagerOperatorName(map),
			contractId);
	}

	public static TurnInDeliveryAction TurnInDelivery(StarMap map, string actorId, string contractId)
	{
		var contract = map.ContractRegistry.All.First(entry => entry.Id == contractId);
		var delivery = (DeliveryObjective)contract.Objective;
		return new TurnInDeliveryAction(
			actorId,
			delivery.TurnInPoiId,
			delivery.TurnInFacilityId,
			delivery.TurnInOperatorName,
			contractId);
	}

	public static DeclineContractAction Decline(StarMap map, string actorId, string contractId) =>
		new(
			actorId,
			AdministrativePoiId,
			ManagementFacilityId,
			MapFacilityOperators.ContractOperatorName(map),
			contractId);

	public static void ReevaluateAndComplete(Engine<StarMap, ActorRuntime> engine, string actorId)
	{
		foreach (var completion in ContractReevaluation.ReevaluateFor(
			engine.World,
			engine.ActorRuntimes.For(actorId),
			actorId))
			engine.Commit(completion);
	}
}
