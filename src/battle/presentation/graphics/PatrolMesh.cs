using Godot;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public static class PatrolMesh
{
	public static int SurfaceIndex(ESpatialOrientation face) => ShipMesh.SurfaceIndex(face);

	public static ArrayMesh CreateHull()
	{
		/*
		 * Same low-poly corvette silhouette as ShipMesh, scaled to ~62%
		 * so deployed patrol craft read smaller than their carrier parent.
		 */

		const float scale = 0.62f;

		var aft = CreateRing(
			z: -0.92f * scale,
			halfWidth: 0.34f * scale,
			topY: 0.22f * scale,
			upperY: 0.11f * scale,
			lowerY: -0.10f * scale,
			bottomY: -0.18f * scale);

		var rear = CreateRing(
			z: -0.55f * scale,
			halfWidth: 0.57f * scale,
			topY: 0.34f * scale,
			upperY: 0.16f * scale,
			lowerY: -0.16f * scale,
			bottomY: -0.27f * scale);

		var middle = CreateRing(
			z: 0.12f * scale,
			halfWidth: 0.70f * scale,
			topY: 0.40f * scale,
			upperY: 0.18f * scale,
			lowerY: -0.18f * scale,
			bottomY: -0.34f * scale);

		var fore = CreateRing(
			z: 0.60f * scale,
			halfWidth: 0.48f * scale,
			topY: 0.27f * scale,
			upperY: 0.11f * scale,
			lowerY: -0.13f * scale,
			bottomY: -0.23f * scale);

		var nose = new Vector3(0f, -0.015f * scale, 1.14f * scale);
		var sternCenter = new Vector3(0f, 0.01f * scale, aft.Z);

		var rings = new[] { aft, rear, middle, fore };

		var forward = new List<Vector3>();
		var retro = new List<Vector3>();
		var dorsal = new List<Vector3>();
		var ventral = new List<Vector3>();
		var port = new List<Vector3>();
		var starboard = new List<Vector3>();

		AddTriangle(forward, nose, fore.Top, fore.PortUpper);
		AddTriangle(forward, nose, fore.PortUpper, fore.PortLower);
		AddTriangle(forward, nose, fore.PortLower, fore.Bottom);
		AddTriangle(forward, nose, fore.Bottom, fore.StarboardLower);
		AddTriangle(forward, nose, fore.StarboardLower, fore.StarboardUpper);
		AddTriangle(forward, nose, fore.StarboardUpper, fore.Top);

		AddTriangle(retro, sternCenter, aft.PortUpper, aft.Top);
		AddTriangle(retro, sternCenter, aft.PortLower, aft.PortUpper);
		AddTriangle(retro, sternCenter, aft.Bottom, aft.PortLower);
		AddTriangle(retro, sternCenter, aft.StarboardLower, aft.Bottom);
		AddTriangle(retro, sternCenter, aft.StarboardUpper, aft.StarboardLower);
		AddTriangle(retro, sternCenter, aft.Top, aft.StarboardUpper);

		for (var i = 0; i < rings.Length - 1; i++)
		{
			var from = rings[i];
			var to = rings[i + 1];

			AddQuad(dorsal, from.PortUpper, from.Top, to.Top, to.PortUpper);
			AddQuad(dorsal, from.Top, from.StarboardUpper, to.StarboardUpper, to.Top);
			AddQuad(ventral, from.PortLower, from.Bottom, to.Bottom, to.PortLower);
			AddQuad(ventral, from.Bottom, from.StarboardLower, to.StarboardLower, to.Bottom);
			AddQuad(port, from.PortUpper, from.PortLower, to.PortLower, to.PortUpper);
			AddQuad(starboard, from.StarboardLower, from.StarboardUpper, to.StarboardUpper, to.StarboardLower);
		}

		var mesh = new ArrayMesh();
		AddSurface(mesh, forward, Vector3.Zero);
		AddSurface(mesh, retro, Vector3.Zero);
		AddSurface(mesh, dorsal, Vector3.Zero);
		AddSurface(mesh, ventral, Vector3.Zero);
		AddSurface(mesh, port, Vector3.Zero);
		AddSurface(mesh, starboard, Vector3.Zero);

		return mesh;
	}

	public static ArrayMesh CreateNoseMarker()
	{
		const float scale = 0.62f;
		const float length = 1.8f * scale;

		var tip = new Vector3(0f, 0.015f * scale, length * 0.64f);
		var left = new Vector3(-0.12f * scale, 0.24f * scale, length * 0.42f);
		var right = new Vector3(0.12f * scale, 0.24f * scale, length * 0.42f);
		var rear = new Vector3(0f, 0.24f * scale, length * 0.34f);

		var vertices = new List<Vector3>();
		AddTriangle(vertices, tip, right, left);
		AddTriangle(vertices, left, right, rear);

		var mesh = new ArrayMesh();
		AddSurface(mesh, vertices, new Vector3(0f, 0.2f * scale, length * 0.45f));
		return mesh;
	}

	private static HullRing CreateRing(
		float z,
		float halfWidth,
		float topY,
		float upperY,
		float lowerY,
		float bottomY) =>
		new(
			Z: z,
			Top: new Vector3(0f, topY, z),
			PortUpper: new Vector3(-halfWidth, upperY, z),
			PortLower: new Vector3(-halfWidth, lowerY, z),
			Bottom: new Vector3(0f, bottomY, z),
			StarboardLower: new Vector3(halfWidth, lowerY, z),
			StarboardUpper: new Vector3(halfWidth, upperY, z));

	private static void AddTriangle(List<Vector3> vertices, Vector3 a, Vector3 b, Vector3 c)
	{
		vertices.Add(a);
		vertices.Add(b);
		vertices.Add(c);
	}

	private static void AddQuad(
		List<Vector3> vertices,
		Vector3 a,
		Vector3 b,
		Vector3 c,
		Vector3 d)
	{
		AddTriangle(vertices, a, b, c);
		AddTriangle(vertices, a, c, d);
	}

	private static void AddSurface(ArrayMesh mesh, List<Vector3> sourceVertices, Vector3 interiorPoint)
	{
		var vertices = sourceVertices.ToArray();
		var normals = new Vector3[vertices.Length];

		for (var i = 0; i < vertices.Length; i += 3)
		{
			var a = vertices[i];
			var b = vertices[i + 1];
			var c = vertices[i + 2];
			var triangleCenter = (a + b + c) / 3f;
			var outwardDirection = triangleCenter - interiorPoint;
			var geometricNormal = (b - a).Cross(c - a);

			if (geometricNormal.LengthSquared() < 0.000001f)
				throw new InvalidOperationException($"Degenerate patrol hull triangle at vertex index {i}.");

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
		Vector3 PortUpper,
		Vector3 PortLower,
		Vector3 Bottom,
		Vector3 StarboardLower,
		Vector3 StarboardUpper);
}
