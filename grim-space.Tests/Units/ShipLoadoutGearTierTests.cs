using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Units;

public sealed class ShipLoadoutGearTierTests
{
	[Fact]
	public void FromLoadout_T0Baseline_IsT0()
	{
		var baseline = ShipCatalog.LoadoutForTier(EType.Patrol, EShipGearTier.T0);
		Assert.Equal(EShipGearTier.T0, ShipLoadoutGearTier.FromLoadout(baseline, EType.Patrol));
	}

	[Fact]
	public void FromLoadout_CanonicalTierRolls_MatchRequestedTier()
	{
		Assert.Equal(
			EShipGearTier.T1,
			ShipLoadoutGearTier.FromLoadout(
				ShipCatalog.LoadoutForTier(EType.Patrol, EShipGearTier.T1),
				EType.Patrol));
		Assert.Equal(
			EShipGearTier.T3,
			ShipLoadoutGearTier.FromLoadout(
				ShipCatalog.LoadoutForTier(EType.Patrol, EShipGearTier.T3),
				EType.Patrol));
	}

	[Fact]
	public void FromLoadout_HigherCanonicalTier_NotBelowLowerTier()
	{
		var t1 = ShipLoadoutGearTier.FromLoadout(
			ShipCatalog.LoadoutForTier(EType.Patrol, EShipGearTier.T1),
			EType.Patrol);
		var t3 = ShipLoadoutGearTier.FromLoadout(
			ShipCatalog.LoadoutForTier(EType.Patrol, EShipGearTier.T3),
			EType.Patrol);

		Assert.True(t3 >= t1);
	}
}
