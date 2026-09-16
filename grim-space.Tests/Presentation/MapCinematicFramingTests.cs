using GrimSpace.Math.Camera;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Presentation;

namespace GrimSpace.Tests.Presentation;

public sealed class MapCinematicFramingTests
{
	private static readonly OrbitLimits Limits = new(
		MinDistance: 10f,
		MaxDistance: 14f,
		MinPitch: 0.35f,
		MaxPitch: 0.7f);

	[Fact]
	public void BehindShip_PlacesCameraOppositeTravelDirection()
	{
		var sample = MapPlayerTravelSample.Resolve(
			mapWidth: 32,
			mapHeight: 32,
			committedPosition: new Coord(0, 0, 0),
			tangent: new Coord(1000, 0, 0),
			pendingCourse: null,
			speedPerTick: 1.0);

		var pose = MapCinematicFraming.BehindShip(
			sample.WorldPosition,
			sample.TravelDirection!.Value,
			Limits);

		Assert.True(MathF.Abs(pose.Yaw) > 0.01f);
		Assert.InRange(pose.Distance, Limits.MinDistance, Limits.MaxDistance);
		Assert.InRange(pose.Pitch, Limits.MinPitch, Limits.MaxPitch);
	}

	[Fact]
	public void BootstrapAtPlayer_CentersPivotOnCommittedPosition()
	{
		var sample = MapPlayerTravelSample.Resolve(
			mapWidth: 32,
			mapHeight: 32,
			committedPosition: new Coord(8, 0, 8),
			tangent: null,
			pendingCourse: null,
			speedPerTick: 1.0);

		var pose = MapCinematicFraming.BootstrapAtPlayer(sample, Limits);

		Assert.Equal(sample.WorldPosition.X, pose.Pivot.X);
		Assert.Equal(sample.WorldPosition.Y, pose.Pivot.Y);
		Assert.Equal(sample.WorldPosition.Z, pose.Pivot.Z);
		Assert.InRange(pose.Distance, Limits.MinDistance, Limits.MaxDistance);
	}
}
