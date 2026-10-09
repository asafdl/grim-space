using GrimSpace.World.StarSystem.Objectives;

namespace GrimSpace.Tests.World.StarSystem.Objectives;

[StarSystemTestSuite]
public sealed class ObjectiveSummaryFormatterTests
{
	[Fact]
	public void ToBbcode_PlainSummary_PassesThroughWithoutLinks()
	{
		var summary = new ObjectiveSummaryContent.Plain("Survey the supply chain.");

		var bbcode = summary.ToBbcode();

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

		var bbcode = summary.ToBbcode();

		Assert.Equal(
			"A pirate fleet spotted ambushing ships on route between [url=\"poi-a\"]Refinery[/url] and [url=\"poi-b\"]Storage[/url].",
			bbcode);
	}

	[Fact]
	public void ToBbcode_NearLandmark_QuotesIdsWithColonsForRichTextParser()
	{
		var summary = new ObjectiveSummaryContent.NearLandmark(
			"Search near ",
			"area:border:12:34",
			"the sector rim",
			".");

		var bbcode = summary.ToBbcode();

		Assert.Equal(
			"Search near [url=\"area:border:12:34\"]the sector rim[/url].",
			bbcode);
	}
}
