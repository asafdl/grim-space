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

	public static ArrayMesh CreateFace(Aabb bounds, ESpatialOrientation face)
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
		var halfSize = bounds.Size * 0.5f;
		var clearance = halfSize * ClearanceRatio + Vector3.One * MinimumClearance;
		var radius = halfSize + clearance;

		return bounds.Position + halfSize + new Vector3(
			(float)vertex.x * radius.X,
			(float)vertex.y * radius.Y,
			(float)vertex.z * radius.Z);
	}

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
