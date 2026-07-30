using Godot;
using GrimSpace.Battle.Movement.Enums;

namespace GrimSpace.Battle.Presentation.Graphics;

public static class ShipMesh
{
	public static int SurfaceIndex(ESpatialOrientation face) => (int)face;

	public static ArrayMesh CreateHull()
	{
		const float length = 1.8f;
		const float width = 1.2f;
		const float height = 0.45f;
		const float halfW = width * 0.5f;
		const float halfH = height;
		const float bodyFore = length * 0.15f;
		const float bodyAft = -length * 0.42f;
		const float noseZ = length * 0.5f;

		var portDorsal = new Vector3(-halfW, halfH, bodyFore);
		var starboardDorsal = new Vector3(halfW, halfH, bodyFore);
		var starboardVentral = new Vector3(halfW, -halfH, bodyFore);
		var portVentral = new Vector3(-halfW, -halfH, bodyFore);
		var nose = new Vector3(0f, 0f, noseZ);

		var mesh = new ArrayMesh();

		// Forward (+Z): CCW when viewed from in front of the ship.
		AddSurface(mesh,
		[
			nose, portVentral, starboardVentral,
			nose, starboardVentral, starboardDorsal,
			nose, starboardDorsal, portDorsal,
			nose, portDorsal, portVentral,
		]);

		// Retro (-Z)
		AddSurface(mesh, Quad(
			new Vector3(halfW, -halfH, bodyAft),
			new Vector3(-halfW, -halfH, bodyAft),
			new Vector3(-halfW, halfH, bodyAft),
			new Vector3(halfW, halfH, bodyAft)));

		// Dorsal (+Y)
		AddSurface(mesh, Quad(
			new Vector3(-halfW, halfH, bodyAft),
			new Vector3(halfW, halfH, bodyAft),
			new Vector3(halfW, halfH, bodyFore),
			new Vector3(-halfW, halfH, bodyFore)));

		// Ventral (-Y)
		AddSurface(mesh, Quad(
			new Vector3(-halfW, -halfH, bodyFore),
			new Vector3(halfW, -halfH, bodyFore),
			new Vector3(halfW, -halfH, bodyAft),
			new Vector3(-halfW, -halfH, bodyAft)));

		// Port (-X)
		AddSurface(mesh, Quad(
			new Vector3(-halfW, -halfH, bodyAft),
			new Vector3(-halfW, -halfH, bodyFore),
			new Vector3(-halfW, halfH, bodyFore),
			new Vector3(-halfW, halfH, bodyAft)));

		// Starboard (+X)
		AddSurface(mesh, Quad(
			new Vector3(halfW, -halfH, bodyFore),
			new Vector3(halfW, -halfH, bodyAft),
			new Vector3(halfW, halfH, bodyAft),
			new Vector3(halfW, halfH, bodyFore)));

		return mesh;
	}

	public static ArrayMesh CreateNoseMarker()
	{
		const float length = 1.8f;
		var tip = new Vector3(0f, 0f, length * 0.52f);
		var left = new Vector3(-0.12f, 0f, length * 0.38f);
		var right = new Vector3(0.12f, 0f, length * 0.38f);
		var top = new Vector3(0f, 0.1f, length * 0.38f);

		return CreateFromTriangles(
		[
			tip, right, left,
			tip, left, top,
			tip, top, right,
		]);
	}

	private static Vector3[] Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) =>
	[
		a, b, c,
		a, c, d,
	];

	private static void AddSurface(ArrayMesh mesh, Vector3[] vertices)
	{
		var normals = new Vector3[vertices.Length];
		for (var i = 0; i < vertices.Length; i += 3)
		{
			var normal = (vertices[i + 1] - vertices[i]).Cross(vertices[i + 2] - vertices[i]).Normalized();
			normals[i] = normal;
			normals[i + 1] = normal;
			normals[i + 2] = normal;
		}

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices;
		arrays[(int)Mesh.ArrayType.Normal] = normals;
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
	}

	private static ArrayMesh CreateFromTriangles(Vector3[] vertices)
	{
		var mesh = new ArrayMesh();
		AddSurface(mesh, vertices);
		return mesh;
	}
}
