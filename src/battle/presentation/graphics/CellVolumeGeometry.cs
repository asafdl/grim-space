using g3;
using Godot;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

internal static class CellVolumeGeometry
{
	internal readonly record struct Surface(Vector3[] Vertices, Vector3[] Normals);
	internal readonly record struct Settings(
		double SamplesPerCell,
		double PrimitiveRadiusInCells,
		double FieldFalloffInCells,
		int SmoothingRounds,
		double SmoothingAlpha)
	{
		public static Settings Default { get; } = new(5, 0.78, 1.1, 24, 0.4);
	}

	public static Surface Build(Coord origin, IReadOnlyCollection<Coord> cells) =>
		Build(origin, cells, Settings.Default);

	public static Surface Build(
		Coord origin,
		IReadOnlyCollection<Coord> cells,
		Settings settings)
	{
		if (cells.Count == 0)
			return new Surface([], []);

		var primitives = cells
			.Distinct()
			.OrderBy(coord => coord.X)
			.ThenBy(coord => coord.Y)
			.ThenBy(coord => coord.Z)
			.Select(coord => new ImplicitSphere3d
			{
				Origin = ToLocal(origin, coord),
				Radius = WorldMapping.CellSize * settings.PrimitiveRadiusInCells,
			})
			.Cast<BoundedImplicitFunction3d>()
			.ToList();
		var fieldFalloff = WorldMapping.CellSize * settings.FieldFalloffInCells;
		var fields = primitives
			.Select(primitive => new DistanceFieldToSkeletalField
			{
				DistanceField = primitive,
				FalloffDistance = fieldFalloff,
			})
			.Cast<BoundedImplicitFunction3d>()
			.ToList();
		var fluidField = BuildSkeletalBlend(fields, 0, fields.Count);
		var cubeSize = WorldMapping.CellSize / settings.SamplesPerCell;
		var bounds = primitives[0].Bounds();
		foreach (var primitive in primitives.Skip(1))
			bounds.Contain(primitive.Bounds());
		bounds.Expand(fieldFalloff + cubeSize);
		var marchingCubes = new MarchingCubes
		{
			Implicit = fluidField,
			IsoValue = DistanceFieldToSkeletalField.ZeroIsocontour,
			Bounds = bounds,
			CubeSize = cubeSize,
			ParallelCompute = false,
			RootMode = MarchingCubes.RootfindingModes.LerpSteps,
			RootModeSteps = 4,
		};

		marchingCubes.Generate();
		marchingCubes.Mesh.ReverseOrientation();
		var smoother = new MeshIterativeSmooth(
			marchingCubes.Mesh,
			marchingCubes.Mesh.VertexIndices().ToArray(),
			bOwnVertices: true)
		{
			Alpha = settings.SmoothingAlpha,
			Rounds = settings.SmoothingRounds,
			SmoothType = MeshIterativeSmooth.SmoothTypes.MeanValue,
		};
		smoother.Smooth();
		MeshNormals.QuickCompute(marchingCubes.Mesh);
		return ToSurface(marchingCubes.Mesh);
	}

	public static string RelativeCellKey(Coord origin, IEnumerable<Coord> cells) =>
		RelativeCellKey(origin, cells, Settings.Default);

	public static string RelativeCellKey(
		Coord origin,
		IEnumerable<Coord> cells,
		Settings settings)
	{
		var settingsKey = FormattableString.Invariant(
			$"{settings.SamplesPerCell:R},{settings.PrimitiveRadiusInCells:R},{settings.FieldFalloffInCells:R},{settings.SmoothingRounds},{settings.SmoothingAlpha:R}");
		var cellsKey = string.Join(
			'|',
			cells
				.Select(cell => cell - origin)
				.Distinct()
				.OrderBy(cell => cell.X)
				.ThenBy(cell => cell.Y)
				.ThenBy(cell => cell.Z)
				.Select(cell => $"{cell.X},{cell.Y},{cell.Z}"));
		return $"{settingsKey}:{cellsKey}";
	}

	private static BoundedImplicitFunction3d BuildSkeletalBlend(
		IReadOnlyList<BoundedImplicitFunction3d> shapes,
		int start,
		int count)
	{
		if (count == 1)
			return shapes[start];

		var leftCount = count / 2;
		return new SkeletalBlend3d
		{
			A = BuildSkeletalBlend(shapes, start, leftCount),
			B = BuildSkeletalBlend(shapes, start + leftCount, count - leftCount),
		};
	}

	private static Surface ToSurface(DMesh3 mesh)
	{
		var vertices = new List<Vector3>(mesh.TriangleCount * 3);
		var normals = new List<Vector3>(mesh.TriangleCount * 3);
		foreach (var triangleId in mesh.TriangleIndices())
		{
			var triangle = mesh.GetTriangle(triangleId);
			AddVertex(triangle.a, mesh, vertices, normals);
			AddVertex(triangle.b, mesh, vertices, normals);
			AddVertex(triangle.c, mesh, vertices, normals);
		}

		return new Surface(vertices.ToArray(), normals.ToArray());
	}

	private static void AddVertex(
		int vertexId,
		DMesh3 mesh,
		List<Vector3> vertices,
		List<Vector3> normals)
	{
		var vertex = mesh.GetVertex(vertexId);
		var normal = mesh.GetVertexNormal(vertexId);
		vertices.Add(new Vector3((float)vertex.x, (float)vertex.y, (float)vertex.z));
		normals.Add(new Vector3(normal.x, normal.y, normal.z));
	}

	private static Vector3d ToLocal(Coord origin, Coord coord) =>
		new(
			(coord.X - origin.X) * WorldMapping.CellSize,
			(coord.Y - origin.Y) * WorldMapping.CellSize,
			(coord.Z - origin.Z) * WorldMapping.CellSize);
}
