using Godot;
using GrimSpace.Math.Camera;

namespace GrimSpace.Tests.Math.Camera;

public sealed class OrbitPoseTests
{
	private static readonly OrbitLimits Limits = new(8f, 80f, -1.5f, 1.5f);

	[Fact]
	public void ReanchorAlongView_KeepsCameraAndMovesPivotToFocusDepth()
	{
		var pose = new OrbitPose { Pivot = new Vector3(5f, 0f, 0f), Distance = 10f, Yaw = 0f, Pitch = 0f };
		var camera = pose.CameraPosition();

		pose.ReanchorAlongView(new Vector3(0f, 3f, -20f), Limits);

		AssertApprox(camera, pose.CameraPosition());
		Assert.Equal(30f, pose.Distance, 3);
		AssertApprox(new Vector3(5f, 0f, -20f), pose.Pivot);
	}

	[Fact]
	public void ReanchorAlongView_ClampsDepthWhenFocusIsBehindCamera()
	{
		var pose = new OrbitPose { Pivot = Vector3.Zero, Distance = 10f, Yaw = 0f, Pitch = 0f };
		var camera = pose.CameraPosition();

		pose.ReanchorAlongView(new Vector3(0f, 0f, 50f), Limits);

		AssertApprox(camera, pose.CameraPosition());
		Assert.Equal(Limits.MinDistance, pose.Distance, 3);
	}

	private static void AssertApprox(Vector3 expected, Vector3 actual) =>
		Assert.True(expected.DistanceTo(actual) < 0.001f, $"expected {expected}, got {actual}");
}
