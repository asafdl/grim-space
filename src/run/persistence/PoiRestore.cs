using GrimSpace.World.StarSystem.Generation;
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
			.Select(facility => new Facility(
				facility.Id,
				facility.DisplayName,
				facility.PresentationAnchor,
				facility.ScenePath,
				facility.Operators))
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
}
