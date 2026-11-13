using GrimSpace.Components;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Run.Persistence;
using GrimSpace.Tests.Run;
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
		Assert.Equal(160, rolled.BaseShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(60, rolled.BaseShieldPoints[ESpatialOrientation.Dorsal]);
		Assert.Equal(spec.DefaultBaseHullPoints, rolled.BaseHullPoints);
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

		Assert.Equal(150, loadout.BaseShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(35, loadout.BaseShieldPoints[ESpatialOrientation.Retro]);
		Assert.Equal(130, stackedPercents.BaseShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(230, ChassisBuffCatalog.Apply(
			spec,
			spec.NewDefaultLoadout(),
			[EChassisBuffOffer.HullPercent]).BaseHullPoints);
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

		Assert.Equal(180, loadout.BaseShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(80, loadout.BaseShieldPoints[ESpatialOrientation.Dorsal]);
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

		Assert.Equal(spec.Maneuverability.BaseActionPoints + 1, state.CurrentActionPoints);
		Assert.Equal(spec.Maneuverability.BaseManeuverPoints, state.CurrentManeuverPoints);
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
		Assert.Equal(loadout.BaseHullPoints, restored.Loadout.BaseHullPoints);
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
	public void ShipStats_CompareMaximumValuesAndApplyChassisBonuses()
	{
		var spec = FighterSpec.Instance;
		var baseline = ShipInstance.FromSpec("baseline", spec, spec.NewDefaultLoadout());
		var loadout = ChassisBuffCatalog.Apply(
			spec,
			spec.NewDefaultLoadout(),
			[EChassisBuffOffer.ShieldFlat, EChassisBuffOffer.HeatCapacity, EChassisBuffOffer.ActionPoint]);
		var candidate = ShipInstance.FromSpec("candidate", spec, loadout);
		candidate.CurrentHullPoints = 1;
		candidate.CurrentShieldPoints[ESpatialOrientation.Forward] = 0;

		var stats = ShipRecruitmentDisplay.Stats(candidate, baseline);

		Assert.Contains(stats, stat => stat.Kind == ShipStatKind.Hull && stat.Value == loadout.BaseHullPoints && stat.Delta == 0);
		Assert.Contains(stats, stat => stat.Kind == ShipStatKind.Shield && stat.Face == ESpatialOrientation.Forward && stat.Value == 130 && stat.Delta == 30);
		Assert.Contains(stats, stat => stat.Kind == ShipStatKind.HeatCapacity && stat.Value == 120 && stat.Delta == 20);
		Assert.Contains(stats, stat => stat.Kind == ShipStatKind.ActionPoints && stat.Value == 5 && stat.Delta == 1);
		Assert.All(ShipRecruitmentDisplay.Stats(candidate), stat => Assert.Null(stat.Delta));
	}

	[Fact]
	public void BrokerBaseline_UsesHighestTierAssignedShipOfSameChassis()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration("assigned-t1", EType.Fighter, EShipGearTier.T1)));
		Assert.True(run.TryAcquireOwnedShip(new ShipSpawnDeclaration("unassigned-t3", EType.Fighter, EShipGearTier.T3)));

		var baseline = ShipRecruitmentDisplay.BrokerBaseline(run, EType.Fighter, new Random(1));

		Assert.NotNull(baseline);
		Assert.Equal("assigned-t1", baseline.Id);
		Assert.Null(ShipRecruitmentDisplay.BrokerBaseline(run, EType.Rogue, new Random(1)));
	}

	[Fact]
	public void BrokerBaseline_RandomlySelectsAmongHighestTierTies()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration("assigned-a", EType.Fighter, EShipGearTier.T1)));
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration("assigned-b", EType.Fighter, EShipGearTier.T1)));
		var random = new Random(1);

		var choices = Enumerable.Range(0, 20)
			.Select(_ => ShipRecruitmentDisplay.BrokerBaseline(run, EType.Fighter, random)!.Id)
			.ToHashSet();

		Assert.Equal(new HashSet<string> { "assigned-a", "assigned-b" }, choices);
	}

	[Fact]
	public void OfferTooltip_ShowsRolledStatsAndAddsComparisonOnlyWithBaseline()
	{
		var offer = new RecruitmentOffer(EType.Fighter, EShipGearTier.T1, ResourceBundle.Empty, RollSeed: 7);
		var rolled = ShipCatalog.LoadoutForTier(offer.Chassis, offer.GearTier, offer.RollSeed);
		var baseline = ShipInstance.FromCatalog("baseline", offer.Chassis);

		var normal = ShipRecruitmentDisplay.OfferTooltip(offer);
		var comparison = ShipRecruitmentDisplay.OfferTooltip(offer, baseline);

		Assert.Null(normal.Comparison);
		Assert.NotNull(normal.Rows);
		Assert.True(normal.TwoColumnRows);
		Assert.Equal(rolled.BaseHullPoints, int.Parse(normal.Rows[0].Value));
		Assert.NotNull(normal.InlineRows);
		var shields = Assert.Single(normal.InlineRows).Values;
		Assert.Equal(Enum.GetValues<ESpatialOrientation>().Length, shields.Count);
		Assert.All(shields, shield => Assert.Null(shield.Delta));
		Assert.NotNull(comparison.Comparison);
		Assert.NotNull(comparison.Comparison.Current.Rows);
		Assert.NotNull(comparison.Comparison.Candidate.Rows);
		Assert.True(comparison.Comparison.Current.TwoColumnRows);
		Assert.True(comparison.Comparison.Candidate.TwoColumnRows);
		Assert.Equal(baseline.Loadout.BaseHullPoints, int.Parse(comparison.Comparison.Current.Rows[0].Value));
		Assert.Equal(rolled.BaseHullPoints - baseline.Loadout.BaseHullPoints, comparison.Comparison.Candidate.Rows[0].Delta);
		Assert.NotNull(comparison.Comparison.Candidate.InlineRows);
		var comparedShields = Assert.Single(comparison.Comparison.Candidate.InlineRows).Values;
		Assert.Equal(
			rolled.BaseShieldPoints[ESpatialOrientation.Forward] - baseline.Loadout.BaseShieldPoints[ESpatialOrientation.Forward],
			comparedShields[0].Delta);
	}
}
