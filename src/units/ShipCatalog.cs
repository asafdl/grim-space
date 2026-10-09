using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;

namespace GrimSpace.Units;

public static class ShipCatalog
{
	public static ShipSpec SpecFor(EType chassis) =>
		chassis switch
		{
			EType.Fighter => FighterSpec.Instance,
			EType.Gunship => GunshipSpec.Instance,
			EType.IndustrialGooper => IndustrialGooperSpec.Instance,
			EType.Carrier => CarrierSpec.Instance,
			EType.RepurposedMiner => RepurposedMinerSpec.Instance,
			EType.VoidBomb => VoidBombSpec.Instance,
			_ => throw new ArgumentOutOfRangeException(nameof(chassis), chassis, null),
		};

	public static ShipLoadout LoadoutForTier(EType chassis, EShipGearTier tier, int? rollSeed = null)
	{
		var baseline = BaselineLoadoutFor(chassis);
		if (tier == EShipGearTier.T0)
			return baseline;

		return ShipLoadoutTierRoller.Roll(chassis, tier, rollSeed, baseline);
	}

	public static ShipLoadout NewRunLoadoutFor(EType chassis) =>
		LoadoutForTier(chassis, EShipGearTier.T0);

	public static ShipLoadout FullFighterLoadout() =>
		FighterSpec.Instance.NewDefaultLoadout();

	public static ShipInstance CreateInstance(string id, EType chassis) =>
		ShipInstance.FromSpec(id, SpecFor(chassis), LoadoutForTier(chassis, EShipGearTier.T0));

	private static ShipLoadout BaselineLoadoutFor(EType chassis) =>
		chassis switch
		{
			EType.Fighter or EType.Gunship or EType.IndustrialGooper or EType.Carrier or EType.RepurposedMiner or EType.VoidBomb =>
				SpecFor(chassis).NewDefaultLoadout(),
			_ => throw new ArgumentOutOfRangeException(nameof(chassis), chassis, null),
		};
}
