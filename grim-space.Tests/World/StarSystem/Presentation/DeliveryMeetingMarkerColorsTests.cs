using GrimSpace.World.StarSystem.Presentation.Map;

namespace grim_space.Tests.World.StarSystem.Presentation;

public sealed class DeliveryMeetingMarkerColorsTests
{
	private static readonly DeliveryMeetingMarkerColors.Rgb PlayerHull =
		new(124f / 255f, 1f, 98f / 255f);

	private static readonly DeliveryMeetingMarkerColors.Rgb ServiceHull =
		new(0.42f, 0.72f, 0.88f);

	[Fact]
	public void Pick_IsDeterministicForMeeting()
	{
		var first = DeliveryMeetingMarkerColors.Pick(42, "meeting-a", ServiceHull, PlayerHull);
		var second = DeliveryMeetingMarkerColors.Pick(42, "meeting-a", ServiceHull, PlayerHull);
		Assert.Equal(first, second);
	}

	[Fact]
	public void Pick_ExcludesColorsSimilarToPlayerAndHull()
	{
		for (var meeting = 0; meeting < 32; meeting++)
		{
			var picked = DeliveryMeetingMarkerColors.Pick(
				7,
				$"meeting-{meeting}",
				ServiceHull,
				PlayerHull);
			Assert.False(IsTooSimilar(picked, PlayerHull));
			Assert.False(IsTooSimilar(picked, ServiceHull));
		}
	}

	private static bool IsTooSimilar(
		DeliveryMeetingMarkerColors.Rgb a,
		DeliveryMeetingMarkerColors.Rgb b)
	{
		var dr = a.R - b.R;
		var dg = a.G - b.G;
		var db = a.B - b.B;
		return dr * dr + dg * dg + db * db < 0.045f;
	}
}
