using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

public static class AsteroidMesh
{
	private const float MinimumBoundaryInsetInCells = 0.06f;
	private const float MaximumBoundaryInsetInCells = 0.28f;
	private const float MaximumInteriorSkewInCells = 0.18f;

	public static ArrayMesh Create(Vector3 size, RandomNumberGenerator rng)
	{
		var half = size * 0.5f;
		var points = new Vector3[3, 3, 3];
		for (var x = -1; x <= 1; x++)
		{
			for (var y = -1; y <= 1; y++)
			{
				for (var z = -1; z <= 1; z++)
				{
					if (x != 0 || y != 0 || z != 0)
						points[x + 1, y + 1, z + 1] = SurfacePoint(x, y, z, half, rng);
				}
			}
		}

		var vertices = new List<Vector3>();
		for (var axis = 0; axis < 3; axis++)
		{
			AddFace(vertices, points, axis, -1);
			AddFace(vertices, points, axis, 1);
		}

		return BuildMesh(vertices);
	}

	private static Vector3 SurfacePoint(
		int x,
		int y,
		int z,
		Vector3 half,
		RandomNumberGenerator rng) =>
		new(
			AxisPosition(x, half.X, rng),
			AxisPosition(y, half.Y, rng),
			AxisPosition(z, half.Z, rng));

	private static float AxisPosition(
		int sign,
		float halfExtent,
		RandomNumberGenerator rng)
	{
		if (sign == 0)
		{
			var skew = Mathf.Min(
				WorldMapping.CellSize * MaximumInteriorSkewInCells,
				halfExtent * 0.3f);
			return rng.RandfRange(-skew, skew);
		}

		var inset = WorldMapping.CellSize * rng.RandfRange(
			MinimumBoundaryInsetInCells,
			MaximumBoundaryInsetInCells);
		return sign * (halfExtent - inset);
	}

	private static void AddFace(
		List<Vector3> vertices,
		Vector3[,,] points,
		int axis,
		int sign)
	{
		for (var u = -1; u < 1; u++)
		{
			for (var v = -1; v < 1; v++)
			{
				AddQuad(
					vertices,
					Point(points, axis, sign, u, v),
					Point(points, axis, sign, u + 1, v),
					Point(points, axis, sign, u + 1, v + 1),
					Point(points, axis, sign, u, v + 1));
			}
		}
	}

	private static Vector3 Point(
		Vector3[,,] points,
		int axis,
		int sign,
		int u,
		int v) =>
		axis switch
		{
			0 => points[sign + 1, u + 1, v + 1],
			1 => points[u + 1, sign + 1, v + 1],
			_ => points[u + 1, v + 1, sign + 1],
		};

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

	private static ArrayMesh BuildMesh(List<Vector3> sourceVertices)
	{
		var vertices = sourceVertices.ToArray();
		var normals = new Vector3[vertices.Length];

		for (var i = 0; i < vertices.Length; i += 3)
		{
			var a = vertices[i];
			var b = vertices[i + 1];
			var c = vertices[i + 2];
			var triangleCenter = (a + b + c) / 3f;
			var geometricNormal = (b - a).Cross(c - a);

			if (geometricNormal.Dot(triangleCenter) > 0f)
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

		var mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		return mesh;
	}
}
