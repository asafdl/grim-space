using GrimSpace.Components;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Run.Persistence;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Presentation.Facilities;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Tests.Units;

public sealed class ChassisBuffTests
{
	[Fact]
	public void UnseededT1_SpendsTheBudgetOnRepeatedShieldFlats()
	{
		var spec = FighterSpec.Instance;
		var rolled = ShipCatalog.LoadoutForTier(EType.Fighter, EShipGearTier.T1);
		var expected = ChassisBuffCatalog.Apply(
			spec,
			spec.NewDefaultLoadout(),
			[EChassisBuffOffer.ShieldFlat, EChassisBuffOffer.ShieldFlat]);

		Assert.Equal(expected.Buffs, rolled.Buffs);
		Assert.Equal(160, rolled.MaxShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(60, rolled.MaxShieldPoints[ESpatialOrientation.Dorsal]);
		Assert.Equal(spec.DefaultMaxHullPoints, rolled.MaxHullPoints);
		Assert.Equal(EShipGearTier.T1, ShipLoadoutGearTier.FromLoadout(rolled, EType.Fighter));
	}

	[Fact]
	public void PercentsInOneCategory_ApplyOnceAfterFlats()
	{
		var spec = FighterSpec.Instance;
		var loadout = ChassisBuffCatalog.Apply(
			spec,
			spec.NewDefaultLoadout(),
			[EChassisBuffOffer.ShieldFlat, EChassisBuffOffer.ShieldPercent]);
		var stackedPercents = ChassisBuffCatalog.Apply(
			spec,
			spec.NewDefaultLoadout(),
			[EChassisBuffOffer.ShieldPercent, EChassisBuffOffer.ShieldPercent]);

		Assert.Equal(150, loadout.MaxShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(35, loadout.MaxShieldPoints[ESpatialOrientation.Retro]);
		Assert.Equal(130, stackedPercents.MaxShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(230, ChassisBuffCatalog.Apply(
			spec,
			spec.NewDefaultLoadout(),
			[EChassisBuffOffer.HullPercent]).MaxHullPoints);
	}

	[Fact]
	public void HeavyShield_BakesFlatAndHeatDownsideTogether()
	{
		var spec = FighterSpec.Instance;
		var loadout = ChassisBuffCatalog.Apply(
			spec,
			spec.NewDefaultLoadout(),
			[EChassisBuffOffer.ShieldFlatHeavy]);
		var state = State.FromShipInstance(ShipInstance.FromSpec("fighter", spec, loadout), Coord.Zero);

		Assert.Equal(180, loadout.MaxShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(80, loadout.MaxShieldPoints[ESpatialOrientation.Dorsal]);
		Assert.Equal(85, state.HeatProfile.EffectiveCapacity);
	}

	[Fact]
	public void RangeOffer_StepsEveryWeaponThatCanTakeIt_AndCountsNetCostOnce()
	{
		var spec = FighterSpec.Instance;
		var loadout = ChassisBuffCatalog.Apply(
			spec,
			spec.NewDefaultLoadout(),
			[EChassisBuffOffer.WeaponRange]);
		var lightning = (LightningCannonSpec)loadout.InstalledAbilities
			.Single(ability => ability.Kind == EAbilityKind.LightningCannon).Spec;
		var bomb = (VoidBombLauncherSpec)loadout.InstalledAbilities
			.Single(ability => ability.Kind == EAbilityKind.VoidBombLauncher).Spec;

		Assert.Equal(LightningCannonSpec.Baseline.LineLength + 1, lightning.LineLength);
		Assert.Equal(VoidBombLauncherSpec.Baseline.FuelTurns + 1, bomb.FuelTurns);
		Assert.Equal(VoidBombLauncherSpec.Baseline.BlastRadius + 1, bomb.BlastRadius);
		Assert.Equal(EShipGearTier.T1, ShipLoadoutGearTier.FromLoadout(loadout, EType.Fighter));
	}

	[Fact]
	public void ActionPointAndHeatRecovery_ApplyWhenStateIsBuilt()
	{
		var spec = FighterSpec.Instance;
		var loadout = ChassisBuffCatalog.Apply(
			spec,
			spec.NewDefaultLoadout(),
			[EChassisBuffOffer.ActionPoint, EChassisBuffOffer.HeatRecovery]);
		var state = State.FromShipInstance(ShipInstance.FromSpec("fighter", spec, loadout), Coord.Zero);

		Assert.Equal(spec.Maneuverability.MaxActionPoints + 1, state.ActionPoints);
		Assert.Equal(spec.Maneuverability.MaxManeuverPoints, state.ManeuverPoints);
		Assert.Equal(24, state.HeatProfile.ClearPerTurn);
		Assert.Equal(100, state.HeatProfile.EffectiveCapacity);
	}

	[Fact]
	public void NoHeatChassis_SkipsHeatBuffsAndHeatDownsides()
	{
		for (var seed = 0; seed < 40; seed++)
		{
			var loadout = ShipCatalog.LoadoutForTier(EType.VoidBomb, EShipGearTier.T3, seed);
			Assert.All(loadout.Buffs, offer =>
			{
				Assert.False(ChassisBuffCatalog.TouchesHeat(offer));
				Assert.False(ChassisBuffCatalog.GrantsRange(offer));
			});
		}
	}

	[Fact]
	public void StrongOffer_IsNotPickedTwice()
	{
		var spec = FighterSpec.Instance;
		var baseline = spec.NewDefaultLoadout();
		var taken = new HashSet<EChassisBuffOffer> { EChassisBuffOffer.WeaponRange };

		Assert.False(ChassisBuffCatalog.IsAvailable(
			EChassisBuffOffer.WeaponRange,
			spec,
			baseline,
			taken,
			rangeStepsTaken: 1));
		Assert.False(ChassisBuffCatalog.IsAvailable(
			EChassisBuffOffer.WeaponRangeHeat,
			spec,
			baseline,
			new HashSet<EChassisBuffOffer>(),
			rangeStepsTaken: 3));
	}

	[Fact]
	public void OfferIds_RoundTripThroughShipPersistence()
	{
		var spec = FighterSpec.Instance;
		var loadout = ChassisBuffCatalog.Apply(
			spec,
			spec.NewDefaultLoadout(),
			[EChassisBuffOffer.WeaponDamageHeavy, EChassisBuffOffer.OverdriveDamage]);
		var ship = ShipInstance.FromSpec("buffed", spec, loadout);
		var restored = SaveDtoMapper.RestoreShip(SaveDtoMapper.CaptureShip(ship));

		Assert.Equal(loadout.Buffs, restored.Loadout.Buffs);
		Assert.Equal(50, restored.Loadout.WeaponDamagePercent);
		Assert.Equal(100, restored.Loadout.OverdriveDamageBonusPercent);
		Assert.Equal(loadout.MaxHullPoints, restored.Loadout.MaxHullPoints);
	}

	[Fact]
	public void RecruitmentOffer_UsesStableListingSeed()
	{
		var offers = StarMarketSeed.Create().ListRecruitmentOffers();
		var again = StarMarketSeed.Create().ListRecruitmentOffers();
		Assert.Equal(offers.Select(offer => offer.RollSeed), again.Select(offer => offer.RollSeed));

		var fighter = offers.Single(offer =>
			offer.Chassis == EType.Fighter && offer.GearTier == EShipGearTier.T1);
		var loadout = ShipCatalog.LoadoutForTier(fighter.Chassis, fighter.GearTier, fighter.RollSeed);
		Assert.NotEmpty(loadout.Buffs);
	}

	[Fact]
	public void OfferTooltip_T0ShowsOnlyBaseStats()
	{
		var offer = new RecruitmentOffer(EType.Fighter, EShipGearTier.T0, ResourceBundle.Empty);

		var tooltip = ShipRecruitmentDisplay.OfferTooltip(offer);

		Assert.Equal("Fighter T0", tooltip.Title);
		Assert.StartsWith("[icon: Hull, text: 200]", tooltip.Body);
		Assert.Contains("[icon: LightningCannon] [icon: Damage, text: 200] [icon: Range, text: 7 km]", tooltip.Body);
		Assert.DoesNotContain(HudTooltipContent.Divider, tooltip.Body);
	}

	[Fact]
	public void OfferTooltip_UpgradedTierShowsNetChangesAgainstSameChassisT0()
	{
		var offers = StarMarketSeed.Create().ListRecruitmentOffers();
		foreach (var offer in offers.Where(offer => offer.GearTier != EShipGearTier.T0))
		{
			var stock = ShipCatalog.LoadoutForTier(offer.Chassis, EShipGearTier.T0);
			var rolled = ShipCatalog.LoadoutForTier(offer.Chassis, offer.GearTier, offer.RollSeed);
			var totals = ChassisBuffCatalog.Sum(rolled.Buffs);

			var tooltip = ShipRecruitmentDisplay.OfferTooltip(offer);

			Assert.EndsWith($" {offer.GearTier}", tooltip.Title);
			var sections = tooltip.Body.Split(HudTooltipContent.Divider);
			Assert.InRange(sections.Length, 2, 3);
			if (rolled.MaxHullPoints != stock.MaxHullPoints)
				Assert.Contains($"[icon: Hull, text: +{rolled.MaxHullPoints - stock.MaxHullPoints}]", tooltip.Body);
			if (totals.WeaponDamagePercent > 0)
				Assert.Contains($"[icon: Damage, text: +{totals.WeaponDamagePercent}%]", tooltip.Body);
			if (totals.ActionPoints > 0)
				Assert.Contains($"[icon: ActionPoints, text: +{totals.ActionPoints}]", tooltip.Body);
			if (totals.ManeuverPoints > 0)
				Assert.Contains($"[icon: ManeuverPoints, text: +{totals.ManeuverPoints}]", tooltip.Body);
		}
	}
}
