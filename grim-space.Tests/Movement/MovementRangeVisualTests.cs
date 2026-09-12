using Godot;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Movement;

public sealed class MovementRangeVisualTests
{
	[Fact]
	public void RangeSurfaceFollowsAsymmetricReach()
	{
		var source = new Coord(5, 5, 5);
		var cells = new HashSet<Coord>
		{
			source - Coord.Forward,
			source + Coord.Forward,
			source + Coord.Forward * 2,
			source + Coord.Forward * 3,
		};

		var surface = MovementRangeGeometry.Build(source, cells);

		Assert.NotEmpty(surface.Vertices);
		var forwardExtent = surface.Vertices.Max(vertex => vertex.Z);
		var rearExtent = -surface.Vertices.Min(vertex => vertex.Z);
		Assert.True(forwardExtent > rearExtent + WorldMapping.CellSize);
		Assert.Equal(surface.Vertices.Length, surface.Normals.Length);
	}

	[Fact]
	public void DisconnectedReachDoesNotFillMajorGap()
	{
		var source = Coord.Zero;
		var cells = new HashSet<Coord>
		{
			new(0, 0, 4),
		};

		var surface = MovementRangeGeometry.Build(source, cells);

		Assert.DoesNotContain(
			surface.Vertices,
			vertex => vertex.Z > WorldMapping.CellSize
				&& vertex.Z < WorldMapping.CellSize * 3);
	}

	[Fact]
	public void AdjacentCellsDoNotRetainTheirSharedFace()
	{
		var source = Coord.Zero;
		var surface = MovementRangeGeometry.Build(
			source,
			new HashSet<Coord> { source, source + Coord.Forward });
		var sharedPlane = WorldMapping.CellSize * 0.5f;

		for (var i = 0; i < surface.Vertices.Length; i += 3)
		{
			Assert.False(
				MathF.Abs(surface.Vertices[i].Z - sharedPlane) < 0.00001f
				&& MathF.Abs(surface.Vertices[i + 1].Z - sharedPlane) < 0.00001f
				&& MathF.Abs(surface.Vertices[i + 2].Z - sharedPlane) < 0.00001f);
		}
	}

	[Fact]
	public void LocalGridUsesOnlyLinesForSixNeighboringCells()
	{
		var segments = GridView.CreateNeighborOutlineSegments();

		Assert.Equal(60, segments.Count);
		Assert.All(
			segments,
			segment => Assert.Equal(
				WorldMapping.CellSize,
				segment.From.DistanceTo(segment.To),
				precision: 5));
	}

	[Fact]
	public void LocalGridAddsHatchesToCameraVisibleFacesAndDimsRearEdges()
	{
		var lines = GridView.CreateCameraAwareLocalGridLines(Vector3.One);

		Assert.Equal(60, lines.Count(line => line.Style != GridView.LocalGridLineStyle.Hatch));
		Assert.Equal(36, lines.Count(line => line.Style == GridView.LocalGridLineStyle.Hatch));
		Assert.Contains(lines, line => line.Style == GridView.LocalGridLineStyle.VisibleEdge);
		Assert.Contains(lines, line => line.Style == GridView.LocalGridLineStyle.RearEdge);
	}

	[Fact]
	public void CameraDepthHeadingsUseDotAndCrossHandles()
	{
		Assert.Equal(
			MovementSelection.HeadingHandleKind.TowardCamera,
			MovementSelection.HeadingHandleKindFor(Vector3.Back, Vector3.Back));
		Assert.Equal(
			MovementSelection.HeadingHandleKind.AwayFromCamera,
			MovementSelection.HeadingHandleKindFor(Vector3.Forward, Vector3.Back));
		Assert.Equal(
			MovementSelection.HeadingHandleKind.Arrow,
			MovementSelection.HeadingHandleKindFor(Vector3.Right, Vector3.Back));
	}

	[Fact]
	public void RangeMeshCacheKeyIsOrderAndTranslationIndependent()
	{
		var source = new Coord(2, 3, 4);
		var cells = new[]
		{
			source + Coord.Forward,
			source + Coord.Up,
			source + Coord.Forward,
		};
		var translatedSource = source + new Coord(5, -2, 1);
		var translatedCells = cells.Select(cell => cell + new Coord(5, -2, 1)).Reverse();

		Assert.Equal(
			GridView.RangeMeshKey(source, cells),
			GridView.RangeMeshKey(translatedSource, translatedCells));
	}
}
