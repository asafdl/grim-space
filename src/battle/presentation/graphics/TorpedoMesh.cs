using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

public static class TorpedoMesh
{
	public static ArrayMesh CreateHull()
	{
		var aft = Ring(z: -0.55f, radius: 0.10f);
		var mid = Ring(z: 0.05f, radius: 0.14f);
		var fore = Ring(z: 0.45f, radius: 0.09f);
		var nose = new Vector3(0f, 0f, 0.72f);
		var stern = new Vector3(0f, 0f, aft.Z);

		var vertices = new List<Vector3>();
		Cap(vertices, nose, fore, outward: true);
		Join(vertices, aft, mid);
		Join(vertices, mid, fore);
		Cap(vertices, stern, aft, outward: false);

		var mesh = new ArrayMesh();
		AddSurface(mesh, vertices);
		return mesh;
	}

	private static HullRing Ring(float z, float radius) =>
		new(
			Z: z,
			Top: new Vector3(0f, radius, z),
			Port: new Vector3(-radius, 0f, z),
			Bottom: new Vector3(0f, -radius, z),
			Starboard: new Vector3(radius, 0f, z));

	private static void Cap(List<Vector3> vertices, Vector3 tip, HullRing ring, bool outward)
	{
		if (outward)
		{
			AddTriangle(vertices, tip, ring.Top, ring.Port);
			AddTriangle(vertices, tip, ring.Port, ring.Bottom);
			AddTriangle(vertices, tip, ring.Bottom, ring.Starboard);
			AddTriangle(vertices, tip, ring.Starboard, ring.Top);
			return;
		}

		AddTriangle(vertices, tip, ring.Port, ring.Top);
		AddTriangle(vertices, tip, ring.Bottom, ring.Port);
		AddTriangle(vertices, tip, ring.Starboard, ring.Bottom);
		AddTriangle(vertices, tip, ring.Top, ring.Starboard);
	}

	private static void Join(List<Vector3> vertices, HullRing from, HullRing to)
	{
		AddQuad(vertices, from.Top, from.Port, to.Port, to.Top);
		AddQuad(vertices, from.Port, from.Bottom, to.Bottom, to.Port);
		AddQuad(vertices, from.Bottom, from.Starboard, to.Starboard, to.Bottom);
		AddQuad(vertices, from.Starboard, from.Top, to.Top, to.Starboard);
	}

	private static void AddTriangle(List<Vector3> vertices, Vector3 a, Vector3 b, Vector3 c)
	{
		vertices.Add(a);
		vertices.Add(b);
		vertices.Add(c);
	}

	private static void AddQuad(List<Vector3> vertices, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
	{
		AddTriangle(vertices, a, b, c);
		AddTriangle(vertices, a, c, d);
	}

	private static void AddSurface(ArrayMesh mesh, List<Vector3> sourceVertices)
	{
		var vertices = sourceVertices.ToArray();
		var normals = new Vector3[vertices.Length];
		var interior = Vector3.Zero;

		for (var i = 0; i < vertices.Length; i += 3)
		{
			var a = vertices[i];
			var b = vertices[i + 1];
			var c = vertices[i + 2];
			var triangleCenter = (a + b + c) / 3f;
			var outwardDirection = triangleCenter - interior;
			var geometricNormal = (b - a).Cross(c - a);

			if (geometricNormal.LengthSquared() < 0.000001f)
				throw new InvalidOperationException($"Degenerate torpedo triangle at vertex index {i}.");

			if (geometricNormal.Dot(outwardDirection) > 0f)
			{
				(vertices[i + 1], vertices[i + 2]) = (vertices[i + 2], vertices[i + 1]);
				geometricNormal = -geometricNormal;
			}

			var outwardNormal = -geometricNormal.Normalized();
			normals[i] = outwardNormal;
			normals[i + 1] = outwardNormal;
			normals[i + 2] = outwardNormal;
		}

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices;
		arrays[(int)Mesh.ArrayType.Normal] = normals;
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
	}

	private readonly record struct HullRing(
		float Z,
		Vector3 Top,
		Vector3 Port,
		Vector3 Bottom,
		Vector3 Starboard);
}
