using GrimSpace.Math.Camera;
using GrimSpace.World.StarSystem.Presentation.Camera;

namespace GrimSpace.Tests.Presentation;

[BattleTestSuite]
public sealed class MapZoomNavigationTests
{
	private static readonly OrbitLimits FacadeLimits = new(2f, 7f, 0.26f, 0.79f);
	private static readonly OrbitLimits CinematicLimits = new(9f, 18f, 0.35f, 0.70f);
	private static readonly OrbitLimits OverviewLimits = new(22f, 240f, 0.87f, 1.13f);

	[Fact]
	public void ProportionalStep_IsPositiveForZoomOut()
	{
		var step = MapZoomNavigation.ProportionalStep(12f, CinematicLimits, -1);

		Assert.True(step > 0f);
	}

	[Fact]
	public void ProportionalStep_IsNegativeForZoomIn()
	{
		var step = MapZoomNavigation.ProportionalStep(12f, CinematicLimits, 1);

		Assert.True(step < 0f);
	}

	[Fact]
	public void ProportionalStep_ScalesWithBandWidth()
	{
		var narrow = MapZoomNavigation.ProportionalStep(5f, FacadeLimits, -1);
		var wide = MapZoomNavigation.ProportionalStep(12f, CinematicLimits, -1);

		Assert.True(MathF.Abs(wide) > MathF.Abs(narrow));
	}

	[Fact]
	public void MultiplicativeStep_ScalesWithCurrentDistance()
	{
		var near = MapZoomNavigation.ProportionalStep(
			50f,
			OverviewLimits,
			-1,
			MapZoomNavigation.OverviewStepPolicy);
		var far = MapZoomNavigation.ProportionalStep(
			200f,
			OverviewLimits,
			-1,
			MapZoomNavigation.OverviewStepPolicy);

		Assert.True(MathF.Abs(far) > MathF.Abs(near));
		Assert.InRange(MathF.Abs(near), 4.9f, 5.1f);
		Assert.InRange(MathF.Abs(far), 19.9f, 20.1f);
	}

	[Fact]
	public void ProportionalStep_NeverExceedsRemainingBand()
	{
		var step = MapZoomNavigation.ProportionalStep(
			CinematicLimits.MaxDistance - 0.01f,
			CinematicLimits,
			-1);

		Assert.True(step <= CinematicLimits.MaxDistance - (CinematicLimits.MaxDistance - 0.01f) + 0.001f);
	}

	[Fact]
	public void WouldCrossOutward_TriggersWhenNextStepPassesMax()
	{
		var distance = CinematicLimits.MaxDistance - 0.1f;

		Assert.True(MapZoomNavigation.WouldCrossOutward(distance, CinematicLimits, -1));
	}

	[Fact]
	public void WouldCrossOutward_DoesNotTriggerOneStepBeforeMax()
	{
		var step = MathF.Abs(MapZoomNavigation.UncappedProportionalStep(12f, CinematicLimits, -1));
		var distance = CinematicLimits.MaxDistance - step - 0.01f;

		Assert.False(MapZoomNavigation.WouldCrossOutward(distance, CinematicLimits, -1));
	}

	[Fact]
	public void WouldCrossInward_TriggersWhenNextStepPassesMin()
	{
		var distance = CinematicLimits.MinDistance + 0.1f;

		Assert.True(MapZoomNavigation.WouldCrossInward(distance, CinematicLimits, 1));
	}

	[Fact]
	public void WouldCrossInward_DoesNotTriggerOneStepBeforeMin()
	{
		var step = MathF.Abs(MapZoomNavigation.UncappedProportionalStep(12f, CinematicLimits, 1));
		var distance = CinematicLimits.MinDistance + step + 0.01f;

		Assert.False(MapZoomNavigation.WouldCrossInward(distance, CinematicLimits, 1));
	}

	[Fact]
	public void InteriorDistance_FromMinSide_SitsInsideBand()
	{
		var distance = MapZoomNavigation.InteriorDistance(CinematicLimits, fromMinSide: true);

		Assert.InRange(distance, CinematicLimits.MinDistance, CinematicLimits.MaxDistance);
		Assert.True(distance > CinematicLimits.MinDistance);
	}

	[Fact]
	public void InteriorDistance_FromMaxSide_SitsInsideBand()
	{
		var distance = MapZoomNavigation.InteriorDistance(CinematicLimits, fromMinSide: false);

		Assert.InRange(distance, CinematicLimits.MinDistance, CinematicLimits.MaxDistance);
		Assert.True(distance < CinematicLimits.MaxDistance);
	}

	[Fact]
	public void InteriorDistance_IsAtLeastTwoDetentsFromCutoff()
	{
		var fromMin = MapZoomNavigation.InteriorDistance(CinematicLimits, fromMinSide: true);
		var detent = MathF.Abs(MapZoomNavigation.ProportionalStep(fromMin, CinematicLimits, 1));

		Assert.True(fromMin - CinematicLimits.MinDistance >= detent * 2f - 0.001f);

		var fromMax = MapZoomNavigation.InteriorDistance(CinematicLimits, fromMinSide: false);
		detent = MathF.Abs(MapZoomNavigation.ProportionalStep(fromMax, CinematicLimits, -1));

		Assert.True(CinematicLimits.MaxDistance - fromMax >= detent * 2f - 0.001f);
	}

	[Fact]
	public void ClampSavedDistanceToInterior_PullsBoundaryPoseInward()
	{
		var clamped = MapZoomNavigation.ClampSavedDistanceToInterior(
			CinematicLimits.MaxDistance,
			CinematicLimits);

		Assert.True(clamped < CinematicLimits.MaxDistance);
		Assert.InRange(clamped, CinematicLimits.MinDistance, CinematicLimits.MaxDistance);
	}
}
