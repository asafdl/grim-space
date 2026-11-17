using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Tests.Presentation;

public sealed class GravityWellFlowLayoutTests
{
	[Fact]
	public void RunsJoinMatchingFacesAndStopAtDirectionChanges()
	{
		var right = new Coord(1, 0, 0);
		var faces = new[]
		{
			new GravityWellFace(-right, Coord.Up, right),
			new GravityWellFace(Coord.Zero, Coord.Up, right),
			new GravityWellFace(right, Coord.Up, -right),
		};

		var runs = GravityWellFlowLayout.Build(faces, Coord.Zero);

		Assert.Contains(new GravityWellFlowRun(right * -3, Coord.Zero, Coord.Up, right), runs);
		Assert.Contains(new GravityWellFlowRun(right * 3, right * 2, Coord.Up, -right), runs);
	}

	[Fact]
	public void RunsRotateWithDomainFacesInEveryCardinalOrientation()
	{
		var center = new Coord(5, 5, 5);
		var grid = new BoundedGrid(11, 11, 11);
		var original = GravityWell.Create("gravitywell-base", center, BodyFrame.WorldAligned(center), grid);
		var baseRuns = GravityWellFlowLayout.Build(original.Faces, center);
		var right = new Coord(1, 0, 0);
		var axes = new[] { right, -right, Coord.Up, -Coord.Up, Coord.Forward, -Coord.Forward };
		foreach (var fore in axes)
		foreach (var dorsal in axes)
		{
			if (Coord.Dot(fore, dorsal) != 0)
				continue;

			var frame = new BodyFrame(center, fore, dorsal, Coord.Cross(dorsal, fore));
			var well = GravityWell.Create("gravitywell-rotated", center, frame, grid);
			Coord Rotate(Coord coord) =>
				frame.Starboard * coord.X + frame.Dorsal * coord.Y + frame.Fore * coord.Z;
			var expected = baseRuns.Select(run => new GravityWellFlowRun(
				Rotate(run.StartHalfCells), Rotate(run.EndHalfCells),
				Rotate(run.Normal), Rotate(run.Heading))).ToHashSet();
			var actual = GravityWellFlowLayout.Build(well.Faces, center).ToHashSet();
			Assert.True(expected.SetEquals(actual));
		}
	}
}
