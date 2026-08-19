using Godot;
using GrimSpace.Math.Camera;

namespace GrimSpace.Tests.Math.Camera;

public sealed class OrbitPoseTests
{
	private static readonly OrbitLimits Limits = new(8f, 100f, -0.5f, 1.2f);

	[Fact]
	public void OffsetPlacesCameraAbovePivotAtPositivePitch()
	{
		var pose = new OrbitPose
		{
			Pivot = Vector3.Zero,
			Yaw = 0f,
			Pitch = Mathf.DegToRad(30f),
			Distance = 10f,
		};

		var offset = pose.Offset();
		Assert.True(offset.Y > 0f);
		Assert.Equal(10f, offset.Length(), precision: 3);
	}

	[Fact]
	public void OrbitClampsPitchToLimits()
	{
		var pose = new OrbitPose { Pitch = 1.0f, Yaw = 0f, Distance = 20f };
		pose.Orbit(new Vector2(0f, -1000f), OrbitControls.OrbitSensitivity, Limits);
		Assert.Equal(Limits.MaxPitch, pose.Pitch);
	}

	[Fact]
	public void ZoomClampsDistance()
	{
		var pose = new OrbitPose { Distance = 90f };
		pose.Zoom(50f, Limits);
		Assert.Equal(Limits.MaxDistance, pose.Distance);
	}

	[Fact]
	public void FlatPanMovesAlongFlattenedForward()
	{
		var pose = new OrbitPose { Pivot = Vector3.Zero };
		pose.FlatPan(new Vector2(0f, 1f), Vector3.Right, Vector3.Forward, speed: 10f, delta: 1f);
		Assert.Equal(0f, pose.Pivot.X, precision: 4);
		Assert.Equal(0f, pose.Pivot.Y, precision: 4);
		Assert.Equal(-10f, pose.Pivot.Z, precision: 4);
	}

	[Fact]
	public void AnglesForDirectionMatchesAtan2Asin()
	{
		var dir = new Vector3(1f, 0.5f, 1f).Normalized();
		var (yaw, pitch) = OrbitPose.AnglesForDirection(dir, Limits);
		Assert.Equal(Mathf.Atan2(dir.X, dir.Z), yaw, precision: 4);
		Assert.Equal(Mathf.Asin(dir.Y), pitch, precision: 4);
	}
}
