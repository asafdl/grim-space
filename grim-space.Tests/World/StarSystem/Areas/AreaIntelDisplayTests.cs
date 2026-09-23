using GrimSpace.World.StarSystem.Areas;

namespace GrimSpace.Tests.World.StarSystem.Areas;

[StarSystemTestSuite]
public sealed class AreaIntelDisplayTests
{
	[Fact]
	public void FormatPlain_SubstitutesResolvedDisplayNames()
	{
		var intel = new AreaIntel("Between {A} and {B}.", "poi-a", "poi-b");

		var text = AreaIntelDisplay.FormatPlain(
			intel,
			id => id switch
			{
				"poi-a" => "Refinery",
				"poi-b" => "Storage",
				_ => null,
			});

		Assert.Equal("Between Refinery and Storage.", text);
	}

	[Fact]
	public void TryParseLinkableSegments_SplitsTemplateAroundPlaceholders()
	{
		var intel = new AreaIntel("Somewhere along the {A}–{B} axis.", "poi-a", "poi-b");

		Assert.True(AreaIntelDisplay.TryParseLinkableSegments(intel, out var segments));
		Assert.Equal("Somewhere along the ", segments.Prefix);
		Assert.Equal("poi-a", segments.LandmarkAId);
		Assert.Equal("–", segments.Connector);
		Assert.Equal("poi-b", segments.LandmarkBId);
		Assert.Equal(" axis.", segments.Suffix);
	}

	[Fact]
	public void TryParseLinkableSegments_RejectsTemplatesWithoutOrderedPlaceholders()
	{
		var intel = new AreaIntel("No landmarks here.", "poi-a", "poi-b");

		Assert.False(AreaIntelDisplay.TryParseLinkableSegments(intel, out _));
	}
}
