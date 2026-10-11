using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

public static class ShipRecruitmentCatalog
{
	public const int RepurposedMinerCreditCost = 300;
	public const int GunshipCreditCost = 800;
	public const int GunshipIndustrialCoreCost = 2;

	private static readonly SheetEntry[] TieredInventory =
	[
		new(
			EMerchantAvailabilityBand.T1,
			new Offer(
				EType.RepurposedMiner,
				EShipGearTier.T0,
				ResourceBundle.Of(ResourceId.Credits, RepurposedMinerCreditCost))),
		new(
			EMerchantAvailabilityBand.T2,
			new Offer(
				EType.Gunship,
				EShipGearTier.T0,
				ResourceBundle.Create(
					(ResourceId.Credits, GunshipCreditCost),
					(ResourceId.IndustrialCore, GunshipIndustrialCoreCost)))),
	];

	public sealed record Offer(EType Chassis, EShipGearTier GearTier, ResourceBundle Cost);

	public static IReadOnlyList<Offer> ListFor(EMerchantAvailabilityBand tier) =>
		TieredInventory
			.Where(entry => MerchantCatalog.IncludesSheet(tier, entry.Sheet))
			.Select(entry => entry.Offer)
			.ToArray();

	public static bool TryFind(
		EType chassis,
		EShipGearTier gearTier,
		EMerchantAvailabilityBand tier,
		out Offer offer)
	{
		offer = ListFor(tier).FirstOrDefault(candidate =>
			candidate.Chassis == chassis && candidate.GearTier == gearTier)!;
		return offer is not null;
	}

	private readonly record struct SheetEntry(
		EMerchantAvailabilityBand Sheet,
		Offer Offer);
}
