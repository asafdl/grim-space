using GrimSpace.World.StarSystem.Areas;

namespace GrimSpace.Tests.World.StarSystem.Areas;

[StarSystemTestSuite]
public sealed class AreaIntelDisplayTests
{
	[Fact]
	public void FormatPlain_SubstitutesResolvedDisplayNames()
	{
		var intel = new AreaIntel("Near {A}, toward {B}.", "poi-a", "poi-b", "poi-c");

		var text = AreaIntelDisplay.FormatPlain(
			intel,
			id => id switch
			{
				"poi-a" => "Refinery",
				"poi-b" => "Storage",
				"poi-c" => "Cloud",
				_ => null,
			});

		Assert.Equal("Near Refinery, toward Storage.", text);
	}

	[Fact]
	public void TryParseLinkableSegments_ClosestOnly()
	{
		var intel = new AreaIntel("Near {A}.", "poi-a", "poi-b", "poi-c");

		Assert.True(AreaIntelDisplay.TryParseLinkableSegments(intel, out var segments));
		var closest = Assert.IsType<AreaIntelDisplay.ParsedSegments.ClosestOnly>(segments);
		Assert.Equal("Near ", closest.Prefix);
		Assert.Equal("poi-a", closest.LandmarkAId);
		Assert.Equal(".", closest.Suffix);
	}

	[Fact]
	public void TryParseLinkableSegments_ClosestAndSecondary()
	{
		var intel = new AreaIntel("Near {A}, toward {B}.", "poi-a", "poi-b", "poi-c");

		Assert.True(AreaIntelDisplay.TryParseLinkableSegments(intel, out var segments));
		var pair = Assert.IsType<AreaIntelDisplay.ParsedSegments.ClosestAndSecondary>(segments);
		Assert.Equal(", toward ", pair.Connector);
		Assert.Equal("poi-b", pair.LandmarkBId);
	}

	[Fact]
	public void TryParseLinkableSegments_ClosestSecondaryAndAnchor()
	{
		var intel = new AreaIntel(
			"Near {A}, with {B} lining up — {C} is just the far edge.",
			"poi-a",
			"poi-b",
			"poi-c");

		Assert.True(AreaIntelDisplay.TryParseLinkableSegments(intel, out var segments));
		var triangle = Assert.IsType<AreaIntelDisplay.ParsedSegments.ClosestSecondaryAndAnchor>(segments);
		Assert.Equal("poi-c", triangle.LandmarkCId);
	}

	[Fact]
	public void TryParseLinkableSegments_RejectsTemplatesWithoutClosestPlaceholder()
	{
		var intel = new AreaIntel("No landmarks here.", "poi-a", "poi-b", "poi-c");

		Assert.False(AreaIntelDisplay.TryParseLinkableSegments(intel, out _));
	}
}
