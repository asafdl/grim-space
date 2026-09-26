using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;

namespace GrimSpace.Tests.Units;

public sealed class ShipCatalogLoadoutForTierTests
{
	[Fact]
	public void T0_NoSpends_MatchesBaseline_AndIgnoresRollSeed()
	{
		var baseline = PatrolSpec.Instance.NewDefaultLoadout();
		var t0 = ShipCatalog.LoadoutForTier(EType.Patrol, EShipGearTier.T0);
		var t0Seeded = ShipCatalog.LoadoutForTier(EType.Patrol, EShipGearTier.T0, rollSeed: 99173);

		AssertLoadoutEquivalent(baseline, t0);
		AssertLoadoutEquivalent(t0, t0Seeded);
		Assert.Equal(0, t0.HullUpgradeTier);
		Assert.All(Enum.GetValues<ESpatialOrientation>(), face =>
			Assert.Equal(0, t0.ShieldUpgradeTiers[face]));
	}

	[Fact]
	public void T0_Fighter_MatchesStarterRunLoadout()
	{
		var starter = ShipCatalog.NewRunLoadoutFor(EType.Fighter);
		var t0 = ShipCatalog.LoadoutForTier(EType.Fighter, EShipGearTier.T0);

		AssertLoadoutEquivalent(starter, t0);
		Assert.Equal(2, t0.InstalledAbilities.Count);
		var railgun = (RailgunSpec)t0.InstalledAbilities
			.Single(ability => ability.Spec.Kind == EAbilityKind.Railgun).Spec;
		Assert.Equal(2, railgun.Damage);
	}

	[Fact]
	public void T0_Patrol_MatchesNewDefaultLoadout()
	{
		var expected = PatrolSpec.Instance.NewDefaultLoadout();
		var t0 = ShipCatalog.LoadoutForTier(EType.Patrol, EShipGearTier.T0);

		AssertLoadoutEquivalent(expected, t0);
	}

	private static void AssertLoadoutEquivalent(ShipLoadout expected, ShipLoadout actual)
	{
		Assert.Equal(expected.MaxHullPoints, actual.MaxHullPoints);
		Assert.Equal(expected.HullUpgradeTier, actual.HullUpgradeTier);
		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
		{
			Assert.Equal(expected.MaxShieldPoints[face], actual.MaxShieldPoints[face]);
			Assert.Equal(expected.ShieldUpgradeTiers[face], actual.ShieldUpgradeTiers[face]);
		}

		Assert.Equal(expected.InstalledAbilities.Count, actual.InstalledAbilities.Count);
		foreach (var installed in expected.InstalledAbilities)
		{
			var match = actual.InstalledAbilities.Single(a => a.Mount == installed.Mount);
			Assert.Equal(installed.Spec, match.Spec);
		}
	}

	[Fact]
	public void Deterministic_NoSeed_SameLoadoutTwice()
	{
		var first = ShipCatalog.LoadoutForTier(EType.Patrol, EShipGearTier.T2);
		var second = ShipCatalog.LoadoutForTier(EType.Patrol, EShipGearTier.T2);

		AssertLoadoutEquivalent(first, second);
	}

	[Fact]
	public void Seeded_Reproducible()
	{
		const int seed = 42_001;
		var first = ShipCatalog.LoadoutForTier(EType.Fighter, EShipGearTier.T3, seed);
		var second = ShipCatalog.LoadoutForTier(EType.Fighter, EShipGearTier.T3, seed);

		AssertLoadoutEquivalent(first, second);
	}

	[Fact]
	public void Seeded_CanVary()
	{
		var a = ShipCatalog.LoadoutForTier(EType.Fighter, EShipGearTier.T3, rollSeed: 1);
		var b = ShipCatalog.LoadoutForTier(EType.Fighter, EShipGearTier.T3, rollSeed: 2);

		Assert.False(AreLoadoutEquivalent(a, b));
	}

	[Fact]
	public void BudgetMonotonic_CanonicalPath_IncreasesUpgradeWeight()
	{
		var t0 = UpgradeWeight(ShipCatalog.LoadoutForTier(EType.Patrol, EShipGearTier.T0));
		var t1 = UpgradeWeight(ShipCatalog.LoadoutForTier(EType.Patrol, EShipGearTier.T1));
		var t2 = UpgradeWeight(ShipCatalog.LoadoutForTier(EType.Patrol, EShipGearTier.T2));
		var t3 = UpgradeWeight(ShipCatalog.LoadoutForTier(EType.Patrol, EShipGearTier.T3));

		Assert.True(t1 >= t0);
		Assert.True(t2 >= t1);
		Assert.True(t3 >= t2);
	}

	[Fact]
	public void NeverExceedsCaps_AtT3_WithManySeeds()
	{
		for (var seed = 0; seed < 100; seed++)
		{
			var loadout = ShipCatalog.LoadoutForTier(EType.Fighter, EShipGearTier.T3, seed);
			Assert.InRange(loadout.HullUpgradeTier, 0, ShipLoadout.MaxHullUpgradeTier);
			foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
				Assert.InRange(loadout.ShieldUpgradeTiers[face], 0, ShipLoadout.MaxShieldUpgradeTier);

			foreach (var installed in loadout.InstalledAbilities)
			{
				Assert.InRange(installed.Spec.DamageUpgradeTier, 0, 3);
				Assert.InRange(installed.Spec.RangeUpgradeTier, 0, 3);
			}
		}
	}

	private static bool AreLoadoutEquivalent(ShipLoadout expected, ShipLoadout actual)
	{
		if (expected.MaxHullPoints != actual.MaxHullPoints
			|| expected.HullUpgradeTier != actual.HullUpgradeTier
			|| expected.InstalledAbilities.Count != actual.InstalledAbilities.Count)
			return false;

		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
		{
			if (expected.MaxShieldPoints[face] != actual.MaxShieldPoints[face]
				|| expected.ShieldUpgradeTiers[face] != actual.ShieldUpgradeTiers[face])
				return false;
		}

		foreach (var installed in expected.InstalledAbilities)
		{
			var match = actual.InstalledAbilities.SingleOrDefault(a => a.Mount == installed.Mount);
			if (match is null || match.Spec != installed.Spec)
				return false;
		}

		return true;
	}

	private static int UpgradeWeight(ShipLoadout loadout)
	{
		var weight = loadout.HullUpgradeTier;
		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
			weight += loadout.ShieldUpgradeTiers[face];

		foreach (var installed in loadout.InstalledAbilities)
		{
			weight += installed.Spec.DamageUpgradeTier;
			weight += installed.Spec.RangeUpgradeTier;
		}

		return weight;
	}
}
