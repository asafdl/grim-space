using Godot;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public static class ShipMesh
{
	public static int SurfaceIndex(ESpatialOrientation face) =>
		face switch
		{
			ESpatialOrientation.Forward => 0,
			ESpatialOrientation.Retro => 1,
			ESpatialOrientation.Dorsal => 2,
			ESpatialOrientation.Ventral => 3,
			ESpatialOrientation.Port => 4,
			ESpatialOrientation.Starboard => 5,
			_ => throw new ArgumentOutOfRangeException(nameof(face), face, null),
		};

	public static ArrayMesh CreateHull()
	{
		/*
		 * Six-sided cross-sections give the ship:
		 *
		 *           Top
		 *          /   \
		 * PortUpper     StarboardUpper
		 *     |             |
		 * PortLower     StarboardLower
		 *          \   /
		 *          Bottom
		 *
		 * Connecting several differently sized sections produces a
		 * simple low-poly corvette rather than a flying rectangular box.
		 */

		var aft = CreateRing(
			z: -0.92f,
			halfWidth: 0.34f,
			topY: 0.22f,
			upperY: 0.11f,
			lowerY: -0.10f,
			bottomY: -0.18f);

		var rear = CreateRing(
			z: -0.55f,
			halfWidth: 0.57f,
			topY: 0.34f,
			upperY: 0.16f,
			lowerY: -0.16f,
			bottomY: -0.27f);

		var middle = CreateRing(
			z: 0.12f,
			halfWidth: 0.70f,
			topY: 0.40f,
			upperY: 0.18f,
			lowerY: -0.18f,
			bottomY: -0.34f);

		var fore = CreateRing(
			z: 0.60f,
			halfWidth: 0.48f,
			topY: 0.27f,
			upperY: 0.11f,
			lowerY: -0.13f,
			bottomY: -0.23f);

		var nose = new Vector3(0f, -0.015f, 1.14f);
		var sternCenter = new Vector3(0f, 0.01f, aft.Z);

		var rings = new[] { aft, rear, middle, fore };

		var forward = new List<Vector3>();
		var retro = new List<Vector3>();
		var dorsal = new List<Vector3>();
		var ventral = new List<Vector3>();
		var port = new List<Vector3>();
		var starboard = new List<Vector3>();

		// Forward surface: six facets forming the nose.
		AddTriangle(forward, nose, fore.Top, fore.PortUpper);
		AddTriangle(forward, nose, fore.PortUpper, fore.PortLower);
		AddTriangle(forward, nose, fore.PortLower, fore.Bottom);
		AddTriangle(forward, nose, fore.Bottom, fore.StarboardLower);
		AddTriangle(forward, nose, fore.StarboardLower, fore.StarboardUpper);
		AddTriangle(forward, nose, fore.StarboardUpper, fore.Top);

		// Retro surface: close the stern.
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

			// Dorsal is split around a raised center ridge.
			AddQuad(
				dorsal,
				from.PortUpper,
				from.Top,
				to.Top,
				to.PortUpper);

			AddQuad(
				dorsal,
				from.Top,
				from.StarboardUpper,
				to.StarboardUpper,
				to.Top);

			// Ventral is split around a shallow central keel.
			AddQuad(
				ventral,
				from.PortLower,
				from.Bottom,
				to.Bottom,
				to.PortLower);

			AddQuad(
				ventral,
				from.Bottom,
				from.StarboardLower,
				to.StarboardLower,
				to.Bottom);

			AddQuad(
				port,
				from.PortUpper,
				from.PortLower,
				to.PortLower,
				to.PortUpper);

			AddQuad(
				starboard,
				from.StarboardLower,
				from.StarboardUpper,
				to.StarboardUpper,
				to.StarboardLower);
		}

		var mesh = new ArrayMesh();

		// Keep this order aligned with SurfaceIndex.
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
		const float length = 1.8f;

		var tip = new Vector3(0f, 0.015f, length * 0.64f);
		var left = new Vector3(-0.12f, 0.24f, length * 0.42f);
		var right = new Vector3(0.12f, 0.24f, length * 0.42f);
		var rear = new Vector3(0f, 0.24f, length * 0.34f);

		var vertices = new List<Vector3>();

		AddTriangle(vertices, tip, right, left);
		AddTriangle(vertices, left, right, rear);

		var mesh = new ArrayMesh();
		AddSurface(mesh, vertices, new Vector3(0f, 0.2f, length * 0.45f));

		return mesh;
	}

	private static HullRing CreateRing(
		float z,
		float halfWidth,
		float topY,
		float upperY,
		float lowerY,
		float bottomY)
	{
		return new HullRing(
			Z: z,
			Top: new Vector3(0f, topY, z),
			PortUpper: new Vector3(-halfWidth, upperY, z),
			PortLower: new Vector3(-halfWidth, lowerY, z),
			Bottom: new Vector3(0f, bottomY, z),
			StarboardLower: new Vector3(halfWidth, lowerY, z),
			StarboardUpper: new Vector3(halfWidth, upperY, z));
	}

	private static void AddTriangle(
		List<Vector3> vertices,
		Vector3 a,
		Vector3 b,
		Vector3 c)
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

	private static void AddSurface(
		ArrayMesh mesh,
		List<Vector3> sourceVertices,
		Vector3 interiorPoint)
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
			{
				throw new InvalidOperationException(
					$"Degenerate hull triangle at vertex index {i}.");
			}

			/*
			 * Godot uses clockwise front faces.
			 *
			 * Cross(b - a, c - a) points outward when the triangle is
			 * counterclockwise from outside. In that case, swap B and C
			 * so the triangle becomes clockwise.
			 */
			if (geometricNormal.Dot(outwardDirection) > 0f)
			{
				(vertices[i + 1], vertices[i + 2]) =
					(vertices[i + 2], vertices[i + 1]);

				geometricNormal = -geometricNormal;
			}

			// After clockwise orientation, the cross product points inward.
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
