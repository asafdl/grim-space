using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;

namespace GrimSpace.Run.Persistence;

internal static class PoiRestore
{
	public static PointOfInterest FromDto(
		StarMapPoiDto dto,
		SupplySystemPlan plan)
	{
		ArgumentNullException.ThrowIfNull(dto);
		ArgumentNullException.ThrowIfNull(plan);

		var facilities = dto.Facilities
			.Select(facility => RestoreFacility(facility, plan))
			.ToArray();

		return dto.Kind switch
		{
			nameof(Star) => Star.FromPersistence(dto.Center),
			nameof(OreMine) => OreMine.FromPersistence(plan, dto.Center, facilities),
			nameof(Refinery) => Refinery.FromPersistence(plan, dto.Center, facilities),
			nameof(StorageFacility) =>
				StorageFacility.FromPersistence(plan, dto.Center, facilities),
			nameof(Wormhole) => Wormhole.FromPersistence(plan, dto.Center, facilities),
			nameof(AdministrativeCore) => AdministrativeCore.FromPersistence(
				plan,
				dto.Center,
				dto.PhysicalForm ?? throw new InvalidDataException(
					$"Saved administrative POI '{dto.Id}' is missing its physical form."),
				facilities),
			nameof(TradeHub) => TradeHub.FromPersistence(plan, dto.Center, facilities),
			_ => throw new InvalidDataException(
				$"Unknown saved POI kind '{dto.Kind}'."),
		};
	}

	private static Facility RestoreFacility(FacilitySaveDto facility, SupplySystemPlan plan)
	{
		var healingStationSlot = HealingStationSlot(facility.Id, plan);
		var shipSupportMerchantSlot = ShipSupportMerchantSlot(facility.Id, plan);
		var operators = facility.Operators
			.Select(facilityOperator =>
			{
				if (facilityOperator.SceneSlotId == shipSupportMerchantSlot)
					return facilityOperator with
					{
						Role = EFacilityOperatorRole.Merchant,
						MerchantCatalog = EMerchantCatalog.ShipSupport,
					};
				if (facilityOperator.SceneSlotId == healingStationSlot)
				return facilityOperator with
				{
					Role = EFacilityOperatorRole.HealingStation,
					MerchantCatalog = EMerchantCatalog.Healing,
				};

				return facilityOperator.Role == EFacilityOperatorRole.Contracts
					? facilityOperator with { Role = EFacilityOperatorRole.Dialog }
					: facilityOperator;
			})
			.ToList();
		if (healingStationSlot is not null
			&& operators.All(facilityOperator =>
				!string.Equals(
					facilityOperator.SceneSlotId,
					healingStationSlot,
					StringComparison.Ordinal)))
		{
			operators.Add(new FacilityOperator(
				FacilityOperator.HealingStationName,
				EFacilityOperatorRole.HealingStation,
				healingStationSlot,
				EMerchantCatalog.Healing));
		}

		return new Facility(
			facility.Id,
			facility.DisplayName,
			facility.PresentationAnchor,
			facility.ScenePath,
			operators);
	}

	private static string? HealingStationSlot(string facilityId, SupplySystemPlan plan)
	{
		if (facilityId == Facility.ScopedId(
			    plan.TradeHubPoiId,
			    TradeHub.DockyardFacilitySlug))
			return TradeHub.DockyardHealingStationSceneSlotId;
		if (facilityId == Facility.ScopedId(
			    plan.TradeHubPoiId,
			    TradeHub.MarketFacilitySlug))
			return TradeHub.MarketHealingStationSceneSlotId;
		if (facilityId == Facility.ScopedId(
			    plan.AdministrativePoiId,
			    AdministrativeCore.ManagementFacilitySlug))
			return AdministrativeCore.HealingStationSceneSlotId;
		if (facilityId == Facility.ScopedId(
			    plan.StoragePoiId,
			    StorageFacility.WarehouseFacilitySlug))
			return StorageFacility.HealingStationSceneSlotId;
		if (facilityId == Facility.ScopedId(
			    plan.ExtractionPoiId,
			    OreMine.MineFacilitySlug))
			return OreMine.HealingStationSceneSlotId;
		if (facilityId == Facility.ScopedId(
			    plan.RefineryPoiId,
			    Refinery.RefineryFacilitySlug))
			return Refinery.HealingStationSceneSlotId;
		if (facilityId == Facility.ScopedId(
			    plan.ExitPoiId,
			    Wormhole.TravelFacilitySlug))
			return Wormhole.HealingStationSceneSlotId;

		return null;
	}

	private static string? ShipSupportMerchantSlot(string facilityId, SupplySystemPlan plan) =>
		facilityId == Facility.ScopedId(
			plan.TradeHubPoiId,
			TradeHub.DockyardFacilitySlug)
			? TradeHub.ShieldOperatorSceneSlotId
			: null;
}
