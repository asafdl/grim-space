using Godot;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Math.Camera;

namespace GrimSpace.Tests.Presentation;

[BattleTestSuite]
public sealed class BattleCameraOverviewTests
{
	private static readonly OrbitLimits Limits = new(8f, 80f, -1.5f, 1.5f);
	private static readonly OrbitPose Source = new() { Distance = 22f };

	[Fact]
	public void Overview_ChoosesAnAngleThatDoesNotLookAlongTheBattleAxis()
	{
		Vector3[] points = [new(-10, 0, -10), new(10, 0, 10)];

		var pose = BattleCameraPoses.Overview(points, Source, 55f, 16f / 9f, Limits);

		Assert.True(MathF.Abs(pose.Offset().Normalized().Dot((points[1] - points[0]).Normalized())) < 0.01f);
	}

	[Theory]
	[InlineData(16f / 9f)]
	[InlineData(3f / 4f)]
	public void Overview_FitsShipsInTheUsableViewportWhenWithinZoomLimit(float aspect)
	{
		Vector3[] points = [new(-8, -4, 3), new(7, 5, -6), new(1, 0, 8)];

		var pose = BattleCameraPoses.Overview(points, Source, 55f, aspect, Limits);
		var direction = pose.Offset().Normalized();
		var right = Vector3.Up.Cross(direction).Normalized();
		var up = direction.Cross(right);
		var verticalTan = MathF.Tan(55f * MathF.PI / 360f);
		foreach (var point in points)
		{
			var relative = point - pose.Pivot;
			var depth = pose.Distance - relative.Dot(direction);
			Assert.True(depth > 0);
			Assert.True(MathF.Abs(relative.Dot(right)) / (depth * verticalTan * aspect) <= 0.8f);
			Assert.True(MathF.Abs(relative.Dot(up)) / (depth * verticalTan) <= 0.8f);
		}
	}

	[Fact]
	public void Overview_RespectsZoomLimitForWidelySeparatedShips()
	{
		Vector3[] points = [new(-1000, 0, 0), new(1000, 0, 0)];

		var pose = BattleCameraPoses.Overview(points, Source, 55f, 16f / 9f, Limits);

		Assert.Equal(Limits.MaxDistance, pose.Distance);
	}

	[Fact]
	public void Overview_ZoomsCloserWhenTheCurrentViewIsUnnecessarilyFar()
	{
		var source = Source;
		source.Distance = 40f;

		var pose = BattleCameraPoses.Overview([Vector3.Zero, Vector3.Right], source, 55f, 16f / 9f, Limits);

		Assert.Equal(22f, pose.Distance);
	}

	[Fact]
	public void Overview_KeepsPriorityShipsReadableInsteadOfZoomingOutForADistantShip()
	{
		Vector3[] priority = [Vector3.Zero, new Vector3(10, 0, 0)];
		Vector3[] points = [priority[0], priority[1], new Vector3(200, 0, 0)];

		var pose = BattleCameraPoses.Overview(points, Source, 55f, 16f / 9f, Limits, priority);

		Assert.True(pose.Distance <= 36f);
		Assert.True(pose.Pivot.DistanceTo(priority[0]) < 20f);
		var direction = pose.Offset().Normalized();
		var right = Vector3.Up.Cross(direction).Normalized();
		var up = direction.Cross(right);
		var verticalTan = MathF.Tan(55f * MathF.PI / 360f);
		foreach (var point in priority)
		{
			var relative = point - pose.Pivot;
			var depth = pose.Distance - relative.Dot(direction);
			Assert.True(depth > 0);
			Assert.True(MathF.Abs(relative.Dot(right)) / (depth * verticalTan * 16f / 9f) <= 0.8f);
			Assert.True(MathF.Abs(relative.Dot(up)) / (depth * verticalTan) <= 0.8f);
		}
	}

	[Fact]
	public void Overview_IncludesNearbyShipsWithoutExceedingComfortableDistance()
	{
		Vector3[] priority = [Vector3.Zero, new Vector3(10, 0, 0)];
		Vector3[] points = [priority[0], priority[1], new Vector3(12, 0, 5)];

		var pose = BattleCameraPoses.Overview(points, Source, 55f, 16f / 9f, Limits, priority);
		var direction = pose.Offset().Normalized();
		var right = Vector3.Up.Cross(direction).Normalized();
		var up = direction.Cross(right);
		var verticalTan = MathF.Tan(55f * MathF.PI / 360f);
		foreach (var point in points)
		{
			var relative = point - pose.Pivot;
			var depth = pose.Distance - relative.Dot(direction);
			Assert.True(MathF.Abs(relative.Dot(right)) / (depth * verticalTan * 16f / 9f) <= 0.8f);
			Assert.True(MathF.Abs(relative.Dot(up)) / (depth * verticalTan) <= 0.8f);
		}
		Assert.True(pose.Distance <= 36f);
	}

	[Fact]
	public void Overview_AvoidsAsteroidsBlockingPriorityShips()
	{
		Vector3[] points = [new(-5, 0, 0), new(5, 0, 0)];
		var asteroid = new Aabb(new Vector3(8, -100, -100), new Vector3(100, 200, 200));
		int CountOccluded(Vector3 camera, IReadOnlyList<Vector3> targets) =>
			targets.Count(target => BoardAsteroidView.SegmentIntersectsBounds(camera, target, asteroid));

		var pose = BattleCameraPoses.Overview(points, Source, 55f, 16f / 9f, Limits, points, CountOccluded);

		Assert.Equal(0, CountOccluded(pose.CameraPosition(), points));
	}

	[Fact]
	public void Overview_StillFramesPriorityShipsWhenEveryDirectionIsOccluded()
	{
		Vector3[] points = [Vector3.Zero, new Vector3(10, 0, 0)];

		var pose = BattleCameraPoses.Overview(
			points, Source, 55f, 16f / 9f, Limits, points, (_, targets) => targets.Count);

		Assert.InRange(pose.Distance, Limits.MinDistance, 36f);
		Assert.True(pose.Pivot.DistanceTo(points[0]) < 20f);
	}

	[Theory]
	[InlineData(1.2f)]
	[InlineData(-1.2f)]
	public void Overview_PrefersTheCurrentSideOfTheBattle(float pitch)
	{
		Vector3[] points = [new(-8, -4, 3), new(7, 5, -6), new(1, 0, 8)];
		var source = Source;
		source.Pitch = pitch;

		var pose = BattleCameraPoses.Overview(points, source, 55f, 16f / 9f, Limits);

		Assert.True(pose.Pitch * pitch > 0);
	}

	[Fact]
	public void Overview_PreservesAnOverheadViewForAsymmetricShipPositions()
	{
		Vector3[] points = [new(-13, 6, -6), new(3, -5, -9), new(-4, 7, 9)];
		var source = Source;
		source.Pitch = 1.2f;

		var pose = BattleCameraPoses.Overview(points, source, 55f, 16f / 9f, Limits);

		Assert.True(pose.Pitch > 0);
	}

	[Fact]
	public void Overview_ChangesSidesIfTheCurrentSideIsObstructed()
	{
		Vector3[] points = [new(-5, 0, 0), new(5, 0, 0)];
		var source = Source;
		source.Pitch = 1.2f;

		var pose = BattleCameraPoses.Overview(
			points, source, 55f, 16f / 9f, Limits, points,
			(camera, targets) => camera.Y > 0 ? targets.Count : 0);

		Assert.True(pose.Pitch < 0);
	}

	[Fact]
	public void Overview_AdjustsAngleForAThreeDimensionalBattleAxis()
	{
		Vector3[] points = [new(-10, -10, -10), new(10, 10, 10)];

		var pose = BattleCameraPoses.Overview(points, Source, 55f, 16f / 9f, Limits);

		Assert.True(MathF.Abs(pose.Offset().Normalized().Dot((points[1] - points[0]).Normalized())) < 0.34f);
	}

	[Fact]
	public void Overview_RejectsAnEmptyParticipantSet()
	{
		Assert.Throws<ArgumentException>(() =>
			BattleCameraPoses.Overview([], Source, 55f, 16f / 9f, Limits));
	}
}
