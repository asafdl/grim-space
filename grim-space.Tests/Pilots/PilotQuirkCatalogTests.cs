using GrimSpace.Pilots;

namespace GrimSpace.Tests.Pilots;

[IntegrationTestSuite]
public sealed class PilotQuirkCatalogTests
{
	[Fact]
	public void Get_ThrowsForUnknownQuirk()
	{
		Assert.Throws<KeyNotFoundException>(() =>
			PilotQuirkCatalog.Get((EPilotQuirkId)999));
	}

	[Fact]
	public void RecruitablePools_AreEmpty()
	{
		Assert.Empty(PilotQuirkCatalog.RecruitablePositive);
		Assert.Empty(PilotQuirkCatalog.RecruitableNegative);
	}

	[Fact]
	public void All_EnumeratesRegisteredQuirks()
	{
		var quirks = PilotQuirkCatalog.All.ToArray();

		Assert.Equal(2, quirks.Length);
		Assert.Contains(quirks, quirk => quirk.Id == EPilotQuirkId.Charismatic);
		Assert.Contains(quirks, quirk => quirk.Id == EPilotQuirkId.NeuroDrugAddict);
	}

	[Fact]
	public void Charismatic_IsDeferredPositiveQuirk()
	{
		var definition = PilotQuirkCatalog.Get(EPilotQuirkId.Charismatic);

		Assert.Equal(EPilotQuirkPolarity.Positive, definition.Polarity);
		Assert.Empty(definition.CapabilityIds);
		Assert.Equal("Charismatic", definition.Name);
	}

	[Fact]
	public void NeuroDrugAddict_IsDeferredNegativeQuirk()
	{
		var definition = PilotQuirkCatalog.Get(EPilotQuirkId.NeuroDrugAddict);

		Assert.Equal(EPilotQuirkPolarity.Negative, definition.Polarity);
		Assert.Empty(definition.CapabilityIds);
		Assert.Equal("Neuro-drug Addict", definition.Name);
	}

	[Fact]
	public void RollRecruitableQuirk_ReturnsNullWhilePoolsAreEmpty()
	{
		Assert.Null(PilotQuirkCatalog.RollRecruitableQuirk(
			EPilotQuirkPolarity.Positive,
			new Random(42)));
		Assert.Null(PilotQuirkCatalog.RollRecruitableQuirk(
			EPilotQuirkPolarity.Negative,
			new Random(42)));
	}

	[Fact]
	public void Roll_ReturnsRegisteredQuirksByPolarity()
	{
		var positive = PilotQuirkCatalog.Roll(EPilotQuirkPolarity.Positive, new Random(42));
		var negative = PilotQuirkCatalog.Roll(EPilotQuirkPolarity.Negative, new Random(42));

		Assert.Equal(EPilotQuirkId.Charismatic, positive);
		Assert.Equal(EPilotQuirkId.NeuroDrugAddict, negative);
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
			"Order Test",
			"Ensures capability order is stored as declared.");

		Assert.Equal(
			[
				EPilotCapabilityId.ShieldBoost,
				EPilotCapabilityId.AdjacentDamageBonus,
				EPilotCapabilityId.ScrapRewardBonus,
			],
			definition.CapabilityIds);
	}
}
