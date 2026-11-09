using g3;
using Godot;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

internal static class ShieldBubbleMesh
{
	private const int Subdivisions = 4;
	private const float ClearanceRatio = 0.32f;
	private const float MinimumClearance = 0.1f;

	private static readonly DMesh3 UnitBubble =
		new Sphere3Generator_NormalizedCube
		{
			EdgeVertices = Subdivisions + 1,
			Radius = 1,
		}.Generate().MakeDMesh();

	private static readonly DMesh3 SmoothBubble =
		new Sphere3Generator_NormalizedCube
		{
			EdgeVertices = 25,
			Radius = 1,
		}.Generate().MakeDMesh();

	public static ArrayMesh CreateFace(Aabb bounds, ESpatialOrientation face)
	{
		var (vertices, normals) = PrepareSurface(bounds, face);
		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices;
		arrays[(int)Mesh.ArrayType.Normal] = normals;
		var mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		return mesh;
	}

	internal static (Vector3[] Vertices, Vector3[] Normals) PrepareSurface(Aabb bounds, ESpatialOrientation face)
	{
		var vertices = new List<Vector3>();
		var normals = new List<Vector3>();
		var radius = Radius(bounds);
		foreach (var triangleId in SmoothBubble.TriangleIndices())
		{
			if (SmoothBubble.GetTriangleGroup(triangleId) != FaceGroup(face))
				continue;
			var triangle = SmoothBubble.GetTriangle(triangleId);
			var a = Direction(triangle.a);
			var b = Direction(triangle.b);
			var c = Direction(triangle.c);
			if ((b - a).Cross(c - a).Dot(a) > 0)
				(b, c) = (c, b);
			foreach (var direction in new[] { a, b, c })
			{
				vertices.Add(bounds.GetCenter() + direction * radius);
				normals.Add((direction / radius).Normalized());
			}
		}
		return (vertices.ToArray(), normals.ToArray());

		Vector3 Direction(int index)
		{
			var vertex = SmoothBubble.GetVertex(index);
			return new Vector3((float)vertex.x, (float)vertex.y, (float)vertex.z);
		}
	}

	public static ArrayMesh CreateWireframeFace(Aabb bounds, ESpatialOrientation face)
	{
		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = PrepareFace(bounds, face);

		var mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, arrays);
		return mesh;
	}

	internal static Vector3[] PrepareFace(Aabb bounds, ESpatialOrientation face)
	{
		var edges = new HashSet<MeshEdge>();
		var group = FaceGroup(face);
		foreach (var triangleId in UnitBubble.TriangleIndices())
		{
			if (UnitBubble.GetTriangleGroup(triangleId) != group)
				continue;

			var triangle = UnitBubble.GetTriangle(triangleId);
			edges.Add(MeshEdge.Create(triangle.a, triangle.b));
			edges.Add(MeshEdge.Create(triangle.b, triangle.c));
			edges.Add(MeshEdge.Create(triangle.c, triangle.a));
		}

		return edges
			.SelectMany(edge => new[]
			{
				FitToBounds(UnitBubble.GetVertex(edge.A), bounds),
				FitToBounds(UnitBubble.GetVertex(edge.B), bounds),
			})
			.ToArray();
	}

	private static Vector3 FitToBounds(Vector3d vertex, Aabb bounds)
	{
		var radius = Radius(bounds);

		return bounds.GetCenter() + new Vector3(
			(float)vertex.x * radius.X,
			(float)vertex.y * radius.Y,
			(float)vertex.z * radius.Z);
	}

	internal static Vector3 Radius(Aabb bounds) =>
		bounds.Size * (0.5f * (1f + ClearanceRatio)) + Vector3.One * MinimumClearance;

	private static int FaceGroup(ESpatialOrientation face) =>
		face switch
		{
			ESpatialOrientation.Retro => 0,
			ESpatialOrientation.Forward => 1,
			ESpatialOrientation.Port => 2,
			ESpatialOrientation.Starboard => 3,
			ESpatialOrientation.Ventral => 4,
			ESpatialOrientation.Dorsal => 5,
			_ => throw new ArgumentOutOfRangeException(nameof(face), face, null),
		};

	private readonly record struct MeshEdge(int A, int B)
	{
		public static MeshEdge Create(int a, int b) =>
			a <= b ? new MeshEdge(a, b) : new MeshEdge(b, a);
	}
}
