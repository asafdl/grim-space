using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

public static class ShipMesh
{
	public static ArrayMesh CreateHull()
	{
		const float length = 1.8f;
		const float width = 1.2f;
		const float height = 0.45f;

		var nose = new Vector3(0f, 0f, length * 0.5f);
		var tailCenter = new Vector3(0f, 0f, -length * 0.42f);
		var tailPort = new Vector3(-width * 0.5f, 0f, -length * 0.35f);
		var tailStarboard = new Vector3(width * 0.5f, 0f, -length * 0.35f);
		var dorsal = new Vector3(0f, height, -length * 0.1f);
		var ventral = new Vector3(0f, -height, -length * 0.1f);

		var vertices = new Vector3[]
		{
			nose, tailPort, tailStarboard,
			nose, dorsal, tailPort,
			nose, tailStarboard, dorsal,
			nose, ventral, tailPort,
			nose, tailStarboard, ventral,
			tailPort, dorsal, tailStarboard,
			tailPort, ventral, tailStarboard,
			tailPort, ventral, dorsal,
			tailStarboard, dorsal, ventral,
			tailCenter, tailPort, tailStarboard,
			tailCenter, dorsal, tailPort,
			tailCenter, tailStarboard, dorsal,
			tailCenter, ventral, tailPort,
			tailCenter, tailStarboard, ventral,
		};

		return CreateFromTriangles(vertices);
	}

	public static ArrayMesh CreateNoseMarker()
	{
		const float length = 1.8f;
		var tip = new Vector3(0f, 0f, length * 0.52f);
		var left = new Vector3(-0.12f, 0f, length * 0.38f);
		var right = new Vector3(0.12f, 0f, length * 0.38f);
		var top = new Vector3(0f, 0.1f, length * 0.38f);

		var vertices = new Vector3[]
		{
			tip, left, right,
			tip, top, left,
			tip, right, top,
		};

		return CreateFromTriangles(vertices);
	}

	private static ArrayMesh CreateFromTriangles(Vector3[] vertices)
	{
		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices;

		var mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		return mesh;
	}
}
