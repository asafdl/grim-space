using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

public static class ShipRecruitmentCatalog
{
	public const int RepurposedMinerCreditCost = 300;
	public const int GunshipCreditCost = 800;
	public const int GunshipIndustrialCoreCost = 2;

	private static readonly Offer[] Offers =
	[
		new(
			EType.RepurposedMiner,
			EShipGearTier.T0,
			ResourceBundle.Of(ResourceId.Credits, RepurposedMinerCreditCost)),
		new(
			EType.Gunship,
			EShipGearTier.T0,
			ResourceBundle.Create(
				(ResourceId.Credits, GunshipCreditCost),
				(ResourceId.IndustrialCore, GunshipIndustrialCoreCost))),
	];

	public sealed record Offer(EType Chassis, EShipGearTier GearTier, ResourceBundle Cost);

	public static IReadOnlyList<Offer> All => Offers;

	public static bool TryFind(EType chassis, EShipGearTier gearTier, out Offer offer)
	{
		offer = Offers.FirstOrDefault(candidate =>
			candidate.Chassis == chassis && candidate.GearTier == gearTier)!;
		return offer is not null;
	}
}
