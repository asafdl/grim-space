using GrimSpace.Pilots;

namespace GrimSpace.Tests.Pilots;

[IntegrationTestSuite]
public sealed class PilotQuirkCatalogTests
{
	[Fact]
	public void Get_ReturnsPlaceholderPositiveMetadata()
	{
		var definition = PilotQuirkCatalog.Get(EPilotQuirkId.PlaceholderPositive);

		Assert.Equal(EPilotQuirkId.PlaceholderPositive, definition.Id);
		Assert.Equal(EPilotQuirkPolarity.Positive, definition.Polarity);
		Assert.Empty(definition.CapabilityIds);
		Assert.Equal("[Dev] No positive quirk", definition.Name);
		Assert.Equal("Placeholder until positive quirks are designed.", definition.Description);
	}

	[Fact]
	public void Get_ReturnsPlaceholderNegativeMetadata()
	{
		var definition = PilotQuirkCatalog.Get(EPilotQuirkId.PlaceholderNegative);

		Assert.Equal(EPilotQuirkId.PlaceholderNegative, definition.Id);
		Assert.Equal(EPilotQuirkPolarity.Negative, definition.Polarity);
		Assert.Empty(definition.CapabilityIds);
		Assert.Equal("[Dev] No negative quirk", definition.Name);
		Assert.Equal("Placeholder until negative quirks are designed.", definition.Description);
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

		Assert.Equal(6, quirks.Length);
		Assert.Contains(quirks, quirk => quirk.Id == EPilotQuirkId.PlaceholderPositive);
		Assert.Contains(quirks, quirk => quirk.Id == EPilotQuirkId.PlaceholderNegative);
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
	public void CapabilityIds_PreserveDeclaredOrder()
	{
		var definition = new PilotQuirkDefinition(
			EPilotQuirkId.PlaceholderPositive,
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
