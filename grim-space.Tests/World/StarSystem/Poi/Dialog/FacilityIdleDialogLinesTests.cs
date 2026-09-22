using GrimSpace.Math;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Dialog;

namespace GrimSpace.Tests.World.StarSystem.Poi.Dialog;

public sealed class FacilityIdleDialogLinesTests
{
	[Theory]
	[InlineData(EPresentationAnchor.Market)]
	[InlineData(EPresentationAnchor.Warehouse)]
	[InlineData(EPresentationAnchor.Refinery)]
	[InlineData(EPresentationAnchor.Travel)]
	public void Pick_ReturnsNonEmptyLineForKnownFacilities(EPresentationAnchor anchor)
	{
		var random = new StableRandom(99);
		var line = FacilityIdleDialogLines.Pick(anchor, random);

		Assert.False(string.IsNullOrWhiteSpace(line));
	}

	[Fact]
	public void Pick_IsDeterministicForSameSeed()
	{
		var first = FacilityIdleDialogLines.Pick(EPresentationAnchor.Market, new StableRandom(42));
		var second = FacilityIdleDialogLines.Pick(EPresentationAnchor.Market, new StableRandom(42));

		Assert.Equal(first, second);
	}

	[Fact]
	public void HasPool_CoversAllPresentationAnchors()
	{
		foreach (var anchor in Enum.GetValues<EPresentationAnchor>())
			Assert.True(FacilityIdleDialogLines.HasPool(anchor));
	}
}
