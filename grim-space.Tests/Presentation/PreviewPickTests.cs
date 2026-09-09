using Godot;
using GrimSpace.Battle.Presentation.Picking;

namespace GrimSpace.Tests.Presentation;

public sealed class PreviewPickTests
{
	[Fact]
	public void DistanceToSegmentUsesNearestPointAlongPlume()
	{
		var distance = PreviewPick.DistanceToSegment(
			new Vector2(60, 15),
			new Vector2(10, 10),
			new Vector2(110, 10));

		Assert.Equal(5f, distance);
	}

	[Fact]
	public void DistanceToSegmentClampsBeyondPlumeEnd()
	{
		var distance = PreviewPick.DistanceToSegment(
			new Vector2(115, 14),
			new Vector2(10, 10),
			new Vector2(110, 10));

		Assert.Equal(Mathf.Sqrt(41), distance, precision: 5);
	}
}
