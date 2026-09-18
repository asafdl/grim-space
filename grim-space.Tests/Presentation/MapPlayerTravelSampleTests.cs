using GrimSpace.Math.Grid;
using GrimSpace.Math.Routes;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Presentation;

namespace GrimSpace.Tests.Presentation;

public sealed class MapPlayerTravelSampleTests
{
	[Fact]
	public void Resolve_Stationary_HasNoDirectionAndInactiveTravel()
	{
		var sample = MapPlayerTravelSample.Resolve(
			mapWidth: 32,
			mapHeight: 32,
			committedPosition: new Coord(4, 0, 4),
			tangent: null,
			pendingCourse: null,
			speedPerTick: 1.0);

		Assert.False(sample.IsTravelActiveOrPending);
		Assert.Null(sample.TravelDirection);
	}

	[Fact]
	public void Resolve_PendingCourse_UsesPathTangentAtStart()
	{
		var path = TransitPath.FromPoints(
			[new Coord(0, 0, 0), new Coord(10, 0, 0)],
			[1.0, 1.0]);
		var pending = new PendingCourse(new Coord(10, 0, 0), path);

		var sample = MapPlayerTravelSample.Resolve(
			mapWidth: 32,
			mapHeight: 32,
			committedPosition: new Coord(0, 0, 0),
			tangent: null,
			pendingCourse: pending,
			speedPerTick: 1.0);

		Assert.True(sample.IsTravelActiveOrPending);
		Assert.NotNull(sample.TravelDirection);
		Assert.True(sample.TravelDirection!.Value.X > 0f);
		Assert.Equal(0f, sample.TravelDirection.Value.Y);
		Assert.Equal(0f, sample.TravelDirection.Value.Z);
	}

	[Fact]
	public void Resolve_ActiveJourney_UsesCommittedTangent()
	{
		var sample = MapPlayerTravelSample.Resolve(
			mapWidth: 32,
			mapHeight: 32,
			committedPosition: new Coord(5, 0, 0),
			tangent: new Coord(1000, 0, 0),
			pendingCourse: null,
			speedPerTick: 1.0);

		Assert.True(sample.IsTravelActiveOrPending);
		Assert.NotNull(sample.TravelDirection);
		Assert.True(sample.TravelDirection!.Value.X > 0f);
	}

	[Fact]
	public void Resolve_ContinuousPosition_UsesFractionalWorldCoordinates()
	{
		var continuous = new PiecewiseRouteSample(
			new RouteSample(2.5, 3.5, 1, 0, 1),
			SegmentIndex: 0);

		var sample = MapPlayerTravelSample.Resolve(
			mapWidth: 32,
			mapHeight: 32,
			continuous,
			pendingCourse: null,
			speedPerTick: 1.0);

		var expected = MapMapping.ToWorld(2.5, 3.5, 32, 32);
		Assert.Equal(expected.X, sample.WorldPosition.X, 4);
		Assert.Equal(expected.Y, sample.WorldPosition.Y, 4);
		Assert.Equal(expected.Z, sample.WorldPosition.Z, 4);
		Assert.True(sample.IsTravelActiveOrPending);
	}
}
