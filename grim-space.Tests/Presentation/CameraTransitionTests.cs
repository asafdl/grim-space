using Godot;
using GrimSpace.Math.Camera;

namespace GrimSpace.Tests.Presentation;

public sealed class CameraTransitionTests
{
	[Fact]
	public void SmallPoseChangeUsesMinimumDuration()
	{
		var from = Pose();
		var to = Pose(pivot: new Vector3(1f, 0f, 0f));

		Assert.Equal(CameraTransition.MinDuration, CameraTransition.Duration(from, to));
	}

	[Fact]
	public void LargePoseChangeUsesMaximumDuration()
	{
		var from = Pose();
		var to = Pose(pivot: new Vector3(100f, 0f, 0f));

		Assert.Equal(CameraTransition.MaxDuration, CameraTransition.Duration(from, to));
	}

	[Fact]
	public void RotationUsesShortestAngularDistance()
	{
		var from = Pose(yaw: Mathf.DegToRad(170f));
		var acrossWrap = Pose(yaw: Mathf.DegToRad(-170f));
		var quarterTurn = Pose(yaw: Mathf.DegToRad(80f));

		Assert.Equal(
			CameraTransition.MinDuration,
			CameraTransition.Duration(from, acrossWrap));
		Assert.True(
			CameraTransition.Duration(from, quarterTurn)
			> CameraTransition.Duration(from, acrossWrap));
	}

	[Fact]
	public void ProportionalZoomChangeAffectsDuration()
	{
		var from = Pose(distance: 10f);
		var smallZoom = Pose(distance: 11f);
		var largeZoom = Pose(distance: 20f);

		Assert.Equal(
			CameraTransition.MinDuration,
			CameraTransition.Duration(from, smallZoom));
		Assert.True(
			CameraTransition.Duration(from, largeZoom)
			> CameraTransition.Duration(from, smallZoom));
	}

	private static OrbitPose Pose(
		Vector3? pivot = null,
		float distance = 20f,
		float yaw = 0f,
		float pitch = 0.5f) =>
		new()
		{
			Pivot = pivot ?? Vector3.Zero,
			Distance = distance,
			Yaw = yaw,
			Pitch = pitch,
		};
}
