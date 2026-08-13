using Godot;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public static class CarrierMesh
{
	public static int SurfaceIndex(ESpatialOrientation face) => ShipMesh.SurfaceIndex(face);

	public static ArrayMesh CreateHull()
	{
		/*
		 * Wide flat-deck hull with an extended stern and blunt bow.
		 * Reads as a carrier next to the narrower fighter corvette.
		 */

		var stern = CreateRing(
			z: -1.18f,
			halfWidth: 0.40f,
			topY: 0.16f,
			upperY: 0.10f,
			lowerY: -0.11f,
			bottomY: -0.19f);

		var aftDeck = CreateRing(
			z: -0.82f,
			halfWidth: 0.82f,
			topY: 0.20f,
			upperY: 0.09f,
			lowerY: -0.13f,
			bottomY: -0.22f);

		var midDeck = CreateRing(
			z: -0.15f,
			halfWidth: 1.02f,
			topY: 0.22f,
			upperY: 0.07f,
			lowerY: -0.15f,
			bottomY: -0.26f);

		var foreDeck = CreateRing(
			z: 0.48f,
			halfWidth: 0.78f,
			topY: 0.18f,
			upperY: 0.06f,
			lowerY: -0.13f,
			bottomY: -0.21f);

		var bow = CreateRing(
			z: 0.88f,
			halfWidth: 0.40f,
			topY: 0.12f,
			upperY: 0.04f,
			lowerY: -0.09f,
			bottomY: -0.15f);

		var bowCap = new Vector3(0f, 0.01f, 1.02f);
		var sternCenter = new Vector3(0f, 0.01f, stern.Z);

		var rings = new[] { stern, aftDeck, midDeck, foreDeck, bow };

		var forward = new List<Vector3>();
		var retro = new List<Vector3>();
		var dorsal = new List<Vector3>();
		var ventral = new List<Vector3>();
		var port = new List<Vector3>();
		var starboard = new List<Vector3>();

		AddTriangle(forward, bowCap, bow.Top, bow.PortUpper);
		AddTriangle(forward, bowCap, bow.PortUpper, bow.PortLower);
		AddTriangle(forward, bowCap, bow.PortLower, bow.Bottom);
		AddTriangle(forward, bowCap, bow.Bottom, bow.StarboardLower);
		AddTriangle(forward, bowCap, bow.StarboardLower, bow.StarboardUpper);
		AddTriangle(forward, bowCap, bow.StarboardUpper, bow.Top);

		AddTriangle(retro, sternCenter, stern.PortUpper, stern.Top);
		AddTriangle(retro, sternCenter, stern.PortLower, stern.PortUpper);
		AddTriangle(retro, sternCenter, stern.Bottom, stern.PortLower);
		AddTriangle(retro, sternCenter, stern.StarboardLower, stern.Bottom);
		AddTriangle(retro, sternCenter, stern.StarboardUpper, stern.StarboardLower);
		AddTriangle(retro, sternCenter, stern.Top, stern.StarboardUpper);

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

	public static ArrayMesh CreateIslandMarker()
	{
		var top = new Vector3(0.10f, 0.42f, -0.10f);
		var port = new Vector3(-0.08f, 0.24f, -0.22f);
		var starboard = new Vector3(0.30f, 0.24f, -0.22f);
		var aft = new Vector3(0.10f, 0.24f, -0.34f);

		var vertices = new List<Vector3>();
		AddTriangle(vertices, top, starboard, port);
		AddTriangle(vertices, port, starboard, aft);

		var mesh = new ArrayMesh();
		AddSurface(mesh, vertices, new Vector3(0.12f, 0.28f, -0.24f));
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
				throw new InvalidOperationException($"Degenerate carrier hull triangle at vertex index {i}.");

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
