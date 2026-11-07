using GrimSpace.Pilots.Barks;
using GrimSpace.Pilots;

namespace GrimSpace.Tests.Pilots;

[IntegrationTestSuite]
public sealed class PilotBarkCatalogTests
{
	[Fact]
	public void Flavors_AreExactlyTheSixPersonalityExpressions()
	{
		Assert.Equal(
			[
				EBarkFlavor.Anxious,
				EBarkFlavor.Bloodthirsty,
				EBarkFlavor.Impatient,
				EBarkFlavor.Analytical,
				EBarkFlavor.Entitled,
				EBarkFlavor.Supportive,
			],
			Enum.GetValues<EBarkFlavor>());
	}

	[Fact]
	public void EveryFlavor_HasNonEmptyLines()
	{
		foreach (var flavor in Enum.GetValues<EBarkFlavor>())
		{
			var lines = PilotBarkCatalog.LinesFor(flavor);
			Assert.NotEmpty(lines);
			Assert.All(lines, line => Assert.False(string.IsNullOrWhiteSpace(line)));
		}
	}

	[Theory]
	[InlineData(EBarkFlavor.Anxious)]
	[InlineData(EBarkFlavor.Bloodthirsty)]
	[InlineData(EBarkFlavor.Impatient)]
	[InlineData(EBarkFlavor.Analytical)]
	[InlineData(EBarkFlavor.Entitled)]
	[InlineData(EBarkFlavor.Supportive)]
	public void Roll_ReturnsLineFromFlavorPool(EBarkFlavor flavor)
	{
		var rng = new Random(7);

		var line = PilotBarkCatalog.Roll(flavor, rng);

		Assert.Contains(line, PilotBarkCatalog.LinesFor(flavor));
	}

	[Fact]
	public void LinesFor_RejectsUnknownFlavor()
	{
		Assert.Throws<KeyNotFoundException>(() =>
			PilotBarkCatalog.LinesFor((EBarkFlavor)999));
	}

	[Theory]
	[InlineData(EBarkFlavor.Anxious, EPilotPersonalityAxis.ThreatResponse, false)]
	[InlineData(EBarkFlavor.Bloodthirsty, EPilotPersonalityAxis.ThreatResponse, true)]
	[InlineData(EBarkFlavor.Impatient, EPilotPersonalityAxis.Deliberation, false)]
	[InlineData(EBarkFlavor.Analytical, EPilotPersonalityAxis.Deliberation, true)]
	[InlineData(EBarkFlavor.Entitled, EPilotPersonalityAxis.Allegiance, false)]
	[InlineData(EBarkFlavor.Supportive, EPilotPersonalityAxis.Allegiance, true)]
	public void PersonalityAffinity_MapsEachFlavor(
		EBarkFlavor flavor, EPilotPersonalityAxis axis, bool positiveSide)
	{
		Assert.Equal((axis, positiveSide), PilotBarkCatalog.PersonalityAffinity(flavor));
	}
}
