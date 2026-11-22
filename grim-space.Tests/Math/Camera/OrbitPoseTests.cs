using Godot;
using GrimSpace.Math.Camera;

namespace GrimSpace.Tests.Math.Camera;

public sealed class OrbitPoseTests
{
	private static readonly OrbitLimits Limits = new(8f, 80f, -1.5f, 1.5f);

	[Fact]
	public void ApplyOrbitInversion_ReversesEachAxisIndependently()
	{
		var delta = new Vector2(4f, -7f);

		Assert.Equal(new Vector2(-4f, -7f), OrbitControls.ApplyOrbitInversion(delta, true, false));
		Assert.Equal(new Vector2(4f, 7f), OrbitControls.ApplyOrbitInversion(delta, false, true));
		Assert.Equal(new Vector2(-4f, 7f), OrbitControls.ApplyOrbitInversion(delta, true, true));
	}

	[Fact]
	public void FreeOrbit_KeepsPivotAndDepth()
	{
		var pose = new OrbitPose { Pivot = new Vector3(2f, 1f, -3f), Distance = 12f, Yaw = 0.4f, Pitch = 0.2f };

		var rotation = pose.FreeOrbit(pose.LevelRotation(), new Vector2(40f, 25f), 0.01f);

		AssertApprox(new Vector3(2f, 1f, -3f), pose.Pivot);
		Assert.Equal(12f, pose.CameraPosition().DistanceTo(pose.Pivot), 3);
		AssertApprox(rotation * Vector3.Back * pose.Distance, pose.Offset());
	}

	[Fact]
	public void FreeOrbit_CrossesThePoleInsteadOfClamping()
	{
		var pose = new OrbitPose { Distance = 10f, Yaw = 0f, Pitch = Mathf.DegToRad(80f) };
		var rotation = pose.LevelRotation();

		rotation = pose.FreeOrbit(rotation, new Vector2(0f, -30f), Mathf.DegToRad(1f));

		var view = rotation * Vector3.Back;
		AssertApprox(new Vector3(0f, Mathf.Sin(Mathf.DegToRad(110f)), Mathf.Cos(Mathf.DegToRad(110f))), view);
		AssertApprox(view * pose.Distance, pose.Offset());
	}

	[Fact]
	public void FreeOrbit_MatchesClampedOrbitAtTheHorizon()
	{
		var free = new OrbitPose { Distance = 10f, Yaw = 0.3f, Pitch = 0f };
		var clamped = free;
		var delta = new Vector2(12f, -7f);

		var rotation = free.FreeOrbit(free.LevelRotation(), delta, OrbitControls.OrbitSensitivity);
		clamped.Orbit(delta, OrbitControls.OrbitSensitivity, Limits);

		var expected = clamped.LevelRotation();
		AssertApprox(expected * Vector3.Back, rotation * Vector3.Back);
		AssertApprox(expected * Vector3.Up, rotation * Vector3.Up);
		AssertApprox(clamped.Offset(), free.Offset());
	}

	[Theory]
	[InlineData(0f, 1f)]
	[InlineData(0f, -1f)]
	[InlineData(1f, 1f)]
	public void ViewPan_AfterOrbit_MovesCameraAndPivotInScreenPlane(float x, float y)
	{
		var pose = new OrbitPose { Pivot = new Vector3(2f, 1f, -3f), Distance = 12f, Yaw = 0.4f, Pitch = 1.4f };
		var rotation = pose.FreeOrbit(pose.LevelRotation(), new Vector2(40f, -25f), 0.01f);
		var basis = new Basis(rotation);
		var camera = pose.CameraPosition();
		var pivot = pose.Pivot;
		var input = new Vector2(x, y).Normalized();
		var expectedShift = (basis.X * input.X + basis.Y * input.Y) * 14f;
		var (right, up) = OrbitPose.ViewPanAxes(basis);

		pose.ViewPan(new Vector2(x, y), right, up, 28f, 0.5f);

		AssertApprox(camera + expectedShift, pose.CameraPosition());
		AssertApprox(pivot + expectedShift, pose.Pivot);
		Assert.Equal(0f, (pose.CameraPosition() - camera).Dot(basis.Z), 3);
		Assert.Equal(12f, pose.Distance);
	}

	[Theory]
	[InlineData(20f)]
	[InlineData(-20f)]
	public void FreeLook_TurnsInPlaceFollowingCameraLocalVerticalInput(float mouseY)
	{
		var pose = new OrbitPose { Pivot = new Vector3(2f, 1f, -3f), Distance = 12f, Yaw = 0.4f, Pitch = 1.4f };
		var rotation = pose.LevelRotation() * new Quaternion(Vector3.Back, 0.8f);
		var camera = pose.CameraPosition();
		var up = rotation * Vector3.Up;

		rotation = pose.FreeLook(rotation, new Vector2(0f, mouseY), Mathf.DegToRad(1f));

		AssertApprox(camera, pose.CameraPosition());
		AssertApprox(camera - rotation * Vector3.Back * 12f, pose.Pivot);
		Assert.Equal(-Mathf.Sin(Mathf.DegToRad(mouseY)), (rotation * Vector3.Forward).Dot(up), 3);
		Assert.Equal(12f, pose.Distance);
	}

	[Fact]
	public void TravelThenOrbit_KeepsNewOrbitCenterAndRadius()
	{
		var pose = new OrbitPose { Pivot = new Vector3(2f, 1f, -3f), Distance = 12f, Yaw = 0.4f, Pitch = 0.2f };
		var camera = pose.CameraPosition();
		var rotation = pose.FreeLook(pose.LevelRotation(), new Vector2(20f, -30f), 0.01f);
		var basis = new Basis(rotation);
		var direction = new Vector2(1f, 1f).Normalized();
		var shift = (basis.X * direction.X - basis.Z * direction.Y) * 14f;

		pose.ViewPan(new Vector2(1f, 1f), basis.X, -basis.Z, 28f, 0.5f);
		AssertApprox(camera + shift, pose.CameraPosition());
		var travelPivot = pose.Pivot;
		AssertApprox(pose.CameraPosition() - basis.Z * 12f, travelPivot);

		rotation = pose.FreeOrbit(rotation, new Vector2(15f, 10f), 0.01f);

		AssertApprox(travelPivot, pose.Pivot);
		AssertApprox(travelPivot + rotation * Vector3.Back * 12f, pose.CameraPosition());
		Assert.Equal(12f, pose.Distance);
	}

	private static void AssertApprox(Vector3 expected, Vector3 actual) =>
		Assert.True(expected.DistanceTo(actual) < 0.001f, $"expected {expected}, got {actual}");
}
