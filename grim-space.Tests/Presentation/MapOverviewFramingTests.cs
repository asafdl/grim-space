using GrimSpace.World.StarSystem.Presentation;

namespace GrimSpace.Tests.Presentation;

public sealed class MapOverviewFramingTests
{
	[Fact]
	public void ResolveLimits_CapsMaxDistanceBelowFullMapFit()
	{
		const float boundsHalf = 16f;
		var limits = MapOverviewFraming.ResolveLimits(boundsHalf, boundsHalf, 1920f, 1080f);

		Assert.Equal(22f, limits.MinDistance);
		Assert.InRange(limits.MaxDistance, 32f, 96f);
	}

	[Fact]
	public void Resolve_EnterPoseStaysWithinResolvedLimits()
	{
		const float boundsHalf = 16f;
		var pose = MapOverviewFraming.Resolve(
			default,
			boundsHalf,
			boundsHalf,
			1920f,
			1080f);
		var limits = MapOverviewFraming.ResolveLimits(boundsHalf, boundsHalf, 1920f, 1080f);

		Assert.InRange(pose.Distance, limits.MinDistance, limits.MaxDistance);
	}
}
