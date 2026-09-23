using GrimSpace.World.StarSystem.Objectives;

namespace GrimSpace.Tests.World.StarSystem.Objectives;

[StarSystemTestSuite]
public sealed class ObjectiveSummaryFormatterTests
{
	[Fact]
	public void ToBbcode_PlainSummary_PassesThroughWithoutLinks()
	{
		var summary = new ObjectiveSummaryContent.Plain("Survey the supply chain.");

		var bbcode = ObjectiveSummaryFormatter.ToBbcode(summary);

		Assert.Equal("Survey the supply chain.", bbcode);
		Assert.DoesNotContain("[url=", bbcode);
	}

	[Fact]
	public void ToBbcode_RouteSummary_LinksOnlyLandmarkNames()
	{
		var summary = new ObjectiveSummaryContent.RouteBetweenLandmarks(
			"A pirate fleet spotted ambushing ships on route between ",
			"poi-a",
			"Refinery",
			" and ",
			"poi-b",
			"Storage",
			".");

		var bbcode = ObjectiveSummaryFormatter.ToBbcode(summary);

		Assert.Equal(
			"A pirate fleet spotted ambushing ships on route between [url=poi-a]Refinery[/url] and [url=poi-b]Storage[/url].",
			bbcode);
	}
}
