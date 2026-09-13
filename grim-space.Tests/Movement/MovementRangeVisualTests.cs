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

		var surface = CellVolumeGeometry.Build(source, cells.Append(source).ToHashSet());

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

		var surface = CellVolumeGeometry.Build(source, cells);

		Assert.DoesNotContain(
			surface.Vertices,
			vertex => vertex.Z > WorldMapping.CellSize
				&& vertex.Z < WorldMapping.CellSize * 3);
	}

	[Fact]
	public void AdjacentCellsDoNotRetainTheirSharedFace()
	{
		var source = Coord.Zero;
		var surface = CellVolumeGeometry.Build(
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
	public void CellVolumeDoesNotImplicitlyIncludeOrigin()
	{
		var origin = Coord.Zero;
		var cell = origin + Coord.Forward * 4;

		var surface = CellVolumeGeometry.Build(origin, new HashSet<Coord> { cell });

		Assert.NotEmpty(surface.Vertices);
		Assert.All(
			surface.Vertices,
			vertex => Assert.True(vertex.Z > WorldMapping.CellSize * 3));
	}

	[Fact]
	public void EmptyCellVolumeProducesEmptySurface()
	{
		var surface = CellVolumeGeometry.Build(Coord.Zero, new HashSet<Coord>());

		Assert.Empty(surface.Vertices);
		Assert.Empty(surface.Normals);
	}

	[Fact]
	public void WeaponCellVolumeFitsInsideAnIsolatedGridCell()
	{
		var origin = Coord.Zero;
		var cells = new HashSet<Coord> { origin };

		var movement = CellVolumeGeometry.Build(origin, cells);
		var weapon = CellVolumeGeometry.Build(
			origin,
			cells,
			CellVolumeWireframeSlot.GeometrySettings);

		Assert.NotEmpty(weapon.Vertices);
		Assert.True(
			weapon.Vertices.Max(vertex => vertex.Length())
			< movement.Vertices.Max(vertex => vertex.Length()));
		Assert.All(
			weapon.Vertices,
			vertex => Assert.True(
				Mathf.Abs(vertex.X) < WorldMapping.CellSize * 0.5f
				&& Mathf.Abs(vertex.Y) < WorldMapping.CellSize * 0.5f
				&& Mathf.Abs(vertex.Z) < WorldMapping.CellSize * 0.5f));
		Assert.NotEqual(
			CellVolumeGeometry.RelativeCellKey(origin, cells),
			CellVolumeGeometry.RelativeCellKey(
				origin,
				cells,
				CellVolumeWireframeSlot.GeometrySettings));
	}

	[Fact]
	public void WeaponCellVolumeDoesNotPinchBetweenAdjacentCells()
	{
		var origin = Coord.Zero;
		var cells = Enumerable.Range(0, 8)
			.Select(z => origin + Coord.Forward * z)
			.ToHashSet();
		var surface = CellVolumeGeometry.Build(
			origin,
			cells,
			CellVolumeWireframeSlot.GeometrySettings);
		var centerRadius = Enumerable.Range(1, 6)
			.Average(cell => RadiusNear(surface.Vertices, cell * WorldMapping.CellSize));
		var midpointRadius = Enumerable.Range(1, 6)
			.Average(cell => RadiusNear(
				surface.Vertices,
				(cell + 0.5f) * WorldMapping.CellSize));

		Assert.True(centerRadius > 0f);
		Assert.True(midpointRadius >= centerRadius * 0.9f);
	}

	[Fact]
	public void TurnVolumeSurfaceUsesLowerDensityThanWeaponSurface()
	{
		var origin = new Coord(5, 5, 5);
		var cells = Enumerable.Range(1, 5)
			.Select(distance => origin + Coord.Forward * distance)
			.ToHashSet();

		var weapon = CellVolumeGeometry.Build(
			origin,
			cells,
			CellVolumeWireframeSlot.GeometrySettings);
		var turnVolume = CellVolumeGeometry.Build(
			origin,
			cells,
			TurnVolumeWireframe.GeometrySettings);

		Assert.True(turnVolume.Vertices.Length < weapon.Vertices.Length / 2);
	}

	private static float RadiusNear(IEnumerable<Vector3> vertices, float z) =>
		vertices
			.Where(vertex => MathF.Abs(vertex.Z - z) < WorldMapping.CellSize * 0.11f)
			.Select(vertex => MathF.Sqrt(vertex.X * vertex.X + vertex.Y * vertex.Y))
			.DefaultIfEmpty()
			.Max();

	[Fact]
	public void CellVolumeIsOrderDuplicateAndTranslationIndependent()
	{
		var origin = new Coord(2, 3, 4);
		var cells = new[]
		{
			origin + Coord.Forward,
			origin + Coord.Up,
			origin + Coord.Forward,
		};
		var translation = new Coord(5, -2, 1);
		var translatedOrigin = origin + translation;
		var translatedCells = cells.Select(cell => cell + translation).Reverse().ToArray();

		var surface = CellVolumeGeometry.Build(origin, cells);
		var translatedSurface = CellVolumeGeometry.Build(translatedOrigin, translatedCells);

		Assert.Equal(surface.Vertices, translatedSurface.Vertices);
		Assert.Equal(surface.Normals, translatedSurface.Normals);
		Assert.Equal(
			CellVolumeGeometry.RelativeCellKey(origin, cells),
			CellVolumeGeometry.RelativeCellKey(translatedOrigin, translatedCells));
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
			CellVolumeGeometry.RelativeCellKey(source, cells),
			CellVolumeGeometry.RelativeCellKey(translatedSource, translatedCells));
	}
}
