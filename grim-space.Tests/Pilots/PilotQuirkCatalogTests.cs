using GrimSpace.Pilots;

namespace GrimSpace.Tests.Pilots;

[IntegrationTestSuite]
public sealed class PilotQuirkCatalogTests
{
	[Fact]
	public void Ids_MatchRegisteredQuirks()
	{
		Assert.Equal(
			PilotQuirkCatalog.All.Select(quirk => quirk.Id).OrderBy(id => id),
			PilotQuirkCatalog.Ids.OrderBy(id => id));
	}

	[Fact]
	public void RollQuirks_UsesCatalogPool()
	{
		var rolled = PilotQuirkCatalog.RollQuirks(new CountRandom(2));

		Assert.Equal(2, rolled.Count);
		Assert.Equal(2, rolled.Distinct().Count());
		Assert.All(rolled, id => Assert.Contains(id, PilotQuirkCatalog.Ids));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	public void RollQuirks_RollsExactCountWithoutDuplicates(int count)
	{
		var pool = new[] { EPilotQuirkId.Charismatic, EPilotQuirkId.NeuroDrugAddict };

		var rolled = PilotQuirkCatalog.RollQuirks(pool, new CountRandom(count));

		Assert.Equal(count, rolled.Count);
		Assert.Equal(count, rolled.Distinct().Count());
		Assert.All(rolled, id => Assert.Contains(id, pool));
		Assert.Equal(new[] { EPilotQuirkId.Charismatic, EPilotQuirkId.NeuroDrugAddict }, pool);
	}

	[Fact]
	public void RollQuirks_CapsCountToAvailablePool()
	{
		Assert.Empty(PilotQuirkCatalog.RollQuirks([], new CountRandom(2)));
		Assert.Equal(EPilotQuirkId.Charismatic, Assert.Single(
			PilotQuirkCatalog.RollQuirks([EPilotQuirkId.Charismatic], new CountRandom(2))));
	}

	[Fact]
	public void RollQuirks_IsSeededAndUniformAcrossCounts()
	{
		var pool = new[] { EPilotQuirkId.Charismatic, EPilotQuirkId.NeuroDrugAddict };
		var first = new Random(42);
		var second = new Random(42);
		var counts = new int[3];
		var singlePositive = 0;
		for (var index = 0; index < 12000; index++)
		{
			var rolled = PilotQuirkCatalog.RollQuirks(pool, first);
			Assert.Equal(rolled, PilotQuirkCatalog.RollQuirks(pool, second));
			counts[rolled.Count]++;
			if (rolled.Count == 1 && rolled[0] == EPilotQuirkId.Charismatic)
				singlePositive++;
		}
		Assert.All(counts, count => Assert.InRange(count, 3700, 4300));
		Assert.InRange(singlePositive / (double)counts[1], 0.47, 0.53);
	}

	[Fact]
	public void RollQuirks_RejectsInvalidPoolAndRandomSource()
	{
		Assert.Throws<ArgumentNullException>(() => PilotQuirkCatalog.RollQuirks(null!));
		Assert.Throws<ArgumentException>(() => PilotQuirkCatalog.RollQuirks(
			[EPilotQuirkId.Charismatic, EPilotQuirkId.Charismatic], new Random(42)));
		Assert.Throws<KeyNotFoundException>(() => PilotQuirkCatalog.RollQuirks(
			[(EPilotQuirkId)999], new Random(42)));
	}

	private sealed class CountRandom(int count) : Random
	{
		private bool _countRolled;

		public override int Next(int maxValue)
		{
			if (_countRolled)
				return 0;
			_countRolled = true;
			Assert.Equal(3, maxValue);
			return count;
		}
	}

	[Fact]
	public void Get_ThrowsForUnknownQuirk()
	{
		Assert.Throws<KeyNotFoundException>(() =>
			PilotQuirkCatalog.Get((EPilotQuirkId)999));
	}

	[Fact]
	public void All_EnumeratesRegisteredQuirks()
	{
		var quirks = PilotQuirkCatalog.All.ToArray();

		Assert.Equal(16, quirks.Length);
		Assert.Contains(quirks, quirk => quirk.Id == EPilotQuirkId.Charismatic);
		Assert.Contains(quirks, quirk => quirk.Id == EPilotQuirkId.PirateHater);
		Assert.Contains(quirks, quirk => quirk.Id == EPilotQuirkId.ShieldMaster);
		Assert.Contains(quirks, quirk => quirk.Id == EPilotQuirkId.MetalMaster);
		Assert.Contains(quirks, quirk => quirk.Id == EPilotQuirkId.ICanFixThat);
		Assert.Contains(quirks, quirk => quirk.Id == EPilotQuirkId.TeamPlayer);
		Assert.Contains(quirks, quirk => quirk.Id == EPilotQuirkId.IceGears);
		Assert.Contains(quirks, quirk => quirk.Id == EPilotQuirkId.RadiatorFanatic);
		Assert.Contains(quirks, quirk => quirk.Id == EPilotQuirkId.NeuroDrugAddict);
		Assert.Contains(quirks, quirk => quirk.Id == EPilotQuirkId.BadTechnician);
		Assert.Contains(quirks, quirk => quirk.Id == EPilotQuirkId.WeakWelding);
		Assert.Contains(quirks, quirk => quirk.Id == EPilotQuirkId.RoughPatchJob);
		Assert.Contains(quirks, quirk => quirk.Id == EPilotQuirkId.TriggerHappy);
		Assert.Contains(quirks, quirk => quirk.Id == EPilotQuirkId.Coward);
		Assert.Contains(quirks, quirk => quirk.Id == EPilotQuirkId.FurnaceCore);
		Assert.Contains(quirks, quirk => quirk.Id == EPilotQuirkId.CloggedVents);
	}

	[Fact]
	public void PirateHater_GrantsPirateDamageBonus()
	{
		var definition = PilotQuirkCatalog.Get(EPilotQuirkId.PirateHater);

		Assert.Equal(EPilotQuirkPolarity.Positive, definition.Polarity);
		Assert.Equal([EPilotCapabilityId.PirateDamageBonus], definition.CapabilityIds);
		Assert.Equal("Pirate Hater", definition.Name);
		Assert.Contains(
			PilotSkillPercents.PirateHater.DamageBonusPercent.ToString(),
			definition.Description);
	}

	[Fact]
	public void Charismatic_GrantsFleetSalaryDiscountAndAllegiance()
	{
		var definition = PilotQuirkCatalog.Get(EPilotQuirkId.Charismatic);

		Assert.Equal(EPilotQuirkPolarity.Positive, definition.Polarity);
		Assert.Equal(
			[EPilotCapabilityId.FleetSalaryDiscount],
			definition.CapabilityIds);
		Assert.Equal(new PilotPersonalityModifier(0, 0, 3), definition.PersonalityModifier);
		Assert.Equal("Charismatic", definition.Name);
		Assert.Contains(
			PilotSkillPercents.BeanCounter.FleetSalaryDiscountPercent.ToString(),
			definition.Description);
	}

	[Fact]
	public void NeuroDrugAddict_IsPersonalityOnlyNegativeQuirk()
	{
		var definition = PilotQuirkCatalog.Get(EPilotQuirkId.NeuroDrugAddict);

		Assert.Equal(EPilotQuirkPolarity.Negative, definition.Polarity);
		Assert.Empty(definition.CapabilityIds);
		Assert.Equal(new PilotPersonalityModifier(0, -8, -6), definition.PersonalityModifier);
		Assert.Equal("Neuro-drug Addict", definition.Name);
	}

	[Fact]
	public void TeamPlayer_IsPersonalityOnlyPositiveQuirk()
	{
		var definition = PilotQuirkCatalog.Get(EPilotQuirkId.TeamPlayer);

		Assert.Equal(EPilotQuirkPolarity.Positive, definition.Polarity);
		Assert.Empty(definition.CapabilityIds);
		Assert.Equal(new PilotPersonalityModifier(0, 0, 8), definition.PersonalityModifier);
		Assert.Equal("Team Player", definition.Name);
	}

	[Fact]
	public void TriggerHappy_IsPersonalityOnlyNegativeQuirk()
	{
		var definition = PilotQuirkCatalog.Get(EPilotQuirkId.TriggerHappy);

		Assert.Equal(EPilotQuirkPolarity.Negative, definition.Polarity);
		Assert.Empty(definition.CapabilityIds);
		Assert.Equal(new PilotPersonalityModifier(8, -6, 0), definition.PersonalityModifier);
		Assert.Equal("Trigger Happy", definition.Name);
	}

	[Fact]
	public void Coward_IsPersonalityOnlyNegativeQuirk()
	{
		var definition = PilotQuirkCatalog.Get(EPilotQuirkId.Coward);

		Assert.Equal(EPilotQuirkPolarity.Negative, definition.Polarity);
		Assert.Empty(definition.CapabilityIds);
		Assert.Equal(new PilotPersonalityModifier(-8, 0, -4), definition.PersonalityModifier);
		Assert.Equal("Coward", definition.Name);
	}

	[Fact]
	public void BadTechnician_GrantsBaseShieldPenalty()
	{
		var definition = PilotQuirkCatalog.Get(EPilotQuirkId.BadTechnician);

		Assert.Equal(EPilotQuirkPolarity.Negative, definition.Polarity);
		Assert.Equal([EPilotCapabilityId.BaseShieldPenalty], definition.CapabilityIds);
		Assert.Equal("Bad Technician", definition.Name);
		Assert.Contains(
			PilotSkillPercents.BadTechnician.BaseShieldPenaltyPerFace.ToString(),
			definition.Description);
	}

	[Fact]
	public void ShieldMaster_GrantsBaseShieldBonus()
	{
		var definition = PilotQuirkCatalog.Get(EPilotQuirkId.ShieldMaster);

		Assert.Equal(EPilotQuirkPolarity.Positive, definition.Polarity);
		Assert.Equal([EPilotCapabilityId.BaseShieldBonus], definition.CapabilityIds);
		Assert.Equal("Shield Master", definition.Name);
		Assert.Contains(
			PilotSkillPercents.ShieldMaster.BaseShieldBonusPerFace.ToString(),
			definition.Description);
	}

	[Fact]
	public void MetalMaster_GrantsBaseHullBonus()
	{
		var definition = PilotQuirkCatalog.Get(EPilotQuirkId.MetalMaster);

		Assert.Equal(EPilotQuirkPolarity.Positive, definition.Polarity);
		Assert.Equal([EPilotCapabilityId.BaseHullBonus], definition.CapabilityIds);
		Assert.Equal("Metal Master", definition.Name);
		Assert.Contains(
			PilotSkillPercents.MetalMaster.BaseHullBonus.ToString(),
			definition.Description);
	}

	[Fact]
	public void ICanFixThat_GrantsHealBonus()
	{
		var definition = PilotQuirkCatalog.Get(EPilotQuirkId.ICanFixThat);

		Assert.Equal(EPilotQuirkPolarity.Positive, definition.Polarity);
		Assert.Equal([EPilotCapabilityId.HealBonus], definition.CapabilityIds);
		Assert.Equal("I Can Fix That!", definition.Name);
		Assert.Contains(
			PilotSkillPercents.ICanFixThat.HealBonusPercent.ToString(),
			definition.Description);
	}

	[Fact]
	public void WeakWelding_GrantsBaseHullFlatPenalty()
	{
		var definition = PilotQuirkCatalog.Get(EPilotQuirkId.WeakWelding);

		Assert.Equal(EPilotQuirkPolarity.Negative, definition.Polarity);
		Assert.Equal([EPilotCapabilityId.BaseHullFlatPenalty], definition.CapabilityIds);
		Assert.Equal("Weak Welding", definition.Name);
		Assert.Contains(
			PilotSkillPercents.WeakWelding.BaseHullPenalty.ToString(),
			definition.Description);
	}

	[Fact]
	public void RoughPatchJob_GrantsHealPenalty()
	{
		var definition = PilotQuirkCatalog.Get(EPilotQuirkId.RoughPatchJob);

		Assert.Equal(EPilotQuirkPolarity.Negative, definition.Polarity);
		Assert.Equal([EPilotCapabilityId.HealPenalty], definition.CapabilityIds);
		Assert.Equal("Rough Patch Job", definition.Name);
		Assert.Contains(
			(100 - PilotSkillPercents.RoughPatchJob.HealTakenPercent).ToString(),
			definition.Description);
	}

	[Fact]
	public void IceGears_GrantsHeatGainReduction()
	{
		var definition = PilotQuirkCatalog.Get(EPilotQuirkId.IceGears);

		Assert.Equal(EPilotQuirkPolarity.Positive, definition.Polarity);
		Assert.Equal([EPilotCapabilityId.HeatGainReduction], definition.CapabilityIds);
		Assert.Equal("Ice Gears", definition.Name);
		Assert.Contains(
			PilotSkillPercents.PodRacer.HeatGainReductionPercent.ToString(),
			definition.Description);
	}

	[Fact]
	public void RadiatorFanatic_GrantsHeatClearBonus()
	{
		var definition = PilotQuirkCatalog.Get(EPilotQuirkId.RadiatorFanatic);

		Assert.Equal(EPilotQuirkPolarity.Positive, definition.Polarity);
		Assert.Equal([EPilotCapabilityId.HeatClearBonus], definition.CapabilityIds);
		Assert.Equal("Radiator Fanatic", definition.Name);
		Assert.Contains(
			PilotSkillPercents.RadiatorFanatic.HeatClearBonusPercent.ToString(),
			definition.Description);
	}

	[Fact]
	public void FurnaceCore_GrantsHeatGainBonus()
	{
		var definition = PilotQuirkCatalog.Get(EPilotQuirkId.FurnaceCore);

		Assert.Equal(EPilotQuirkPolarity.Negative, definition.Polarity);
		Assert.Equal([EPilotCapabilityId.HeatGainBonus], definition.CapabilityIds);
		Assert.Equal("Furnace Core", definition.Name);
		Assert.Contains(
			PilotSkillPercents.SynthSinger.HeatGainBonusPercent.ToString(),
			definition.Description);
	}

	[Fact]
	public void CloggedVents_GrantsHeatClearPenalty()
	{
		var definition = PilotQuirkCatalog.Get(EPilotQuirkId.CloggedVents);

		Assert.Equal(EPilotQuirkPolarity.Negative, definition.Polarity);
		Assert.Equal([EPilotCapabilityId.HeatClearPenalty], definition.CapabilityIds);
		Assert.Equal("Clogged Vents", definition.Name);
		Assert.Contains(
			(100 - PilotSkillPercents.CloggedVents.HeatClearTakenPercent).ToString(),
			definition.Description);
	}

	[Fact]
	public void Roll_ReturnsRegisteredQuirksByPolarity()
	{
		var positive = PilotQuirkCatalog.Roll(EPilotQuirkPolarity.Positive, new Random(42));
		var negative = PilotQuirkCatalog.Roll(EPilotQuirkPolarity.Negative, new Random(42));

		Assert.Contains(
			positive,
			(IReadOnlyList<EPilotQuirkId>)[
				EPilotQuirkId.Charismatic,
				EPilotQuirkId.PirateHater,
				EPilotQuirkId.ShieldMaster,
				EPilotQuirkId.MetalMaster,
				EPilotQuirkId.ICanFixThat,
				EPilotQuirkId.TeamPlayer,
				EPilotQuirkId.IceGears,
				EPilotQuirkId.RadiatorFanatic]);
		Assert.Contains(
			negative,
			(IReadOnlyList<EPilotQuirkId>)[
				EPilotQuirkId.NeuroDrugAddict,
				EPilotQuirkId.BadTechnician,
				EPilotQuirkId.WeakWelding,
				EPilotQuirkId.RoughPatchJob,
				EPilotQuirkId.TriggerHappy,
				EPilotQuirkId.Coward,
				EPilotQuirkId.FurnaceCore,
				EPilotQuirkId.CloggedVents]);
	}

	[Fact]
	public void CapabilityIds_PreserveDeclaredOrder()
	{
		var definition = new PilotQuirkDefinition(
			EPilotQuirkId.Charismatic,
			EPilotQuirkPolarity.Positive,
			[
				EPilotCapabilityId.ShieldBoost,
				EPilotCapabilityId.AdjacentDamageBonus,
				EPilotCapabilityId.ScrapRewardBonus,
			],
			new GrimSpace.Components.HudTooltipContent(
				"Order Test",
				"Ensures capability order is stored as declared."));

		Assert.Equal(
			[
				EPilotCapabilityId.ShieldBoost,
				EPilotCapabilityId.AdjacentDamageBonus,
				EPilotCapabilityId.ScrapRewardBonus,
			],
			definition.CapabilityIds);
	}
}
