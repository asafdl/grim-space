using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

internal sealed class CloudFragmentMesh
{
	private readonly Vector3 _center;
	private readonly float _extent;
	private readonly Surface[] _surfaces;

	private sealed record Surface(
		Godot.Collections.Array Arrays,
		Vector3[] Vertices,
		Vector3[] Normals,
		float[] Tangents,
		int[] Indices,
		int[] Roots,
		Vector3[] Centers,
		int[] Counts);

	public CloudFragmentMesh(Mesh source, Vector3 center, float extent)
	{
		if (source is not ArrayMesh mesh)
			throw new InvalidOperationException("Cloud model is not an array mesh.");
		if (extent <= 0f)
			throw new ArgumentOutOfRangeException(nameof(extent));
		_center = center;
		_extent = extent;
		_surfaces = Enumerable.Range(0, mesh.GetSurfaceCount())
			.Select(surface => PrepareSurface(mesh, surface)).ToArray();
		if (_surfaces.Length == 0)
			throw new InvalidOperationException("Cloud model has no surfaces.");
	}

	public Mesh Build(IReadOnlyList<CloudRegion> regions)
	{
		ArgumentNullException.ThrowIfNull(regions);
		var transforms = regions.Select(region => (
			Pivot: _center + ToGodot(region.Center) * _extent,
			Offset: ToGodot(region.Offset) * _extent,
			Rotation: Basis.FromEuler(ToGodot(region.Rotation)))).ToArray();
		var result = new ArrayMesh();
		foreach (var surface in _surfaces)
		{
			var regionByRoot = Enumerable.Repeat(-1, surface.Vertices.Length).ToArray();
			for (var i = 0; i < surface.Counts.Length; i++)
			{
				if (surface.Counts[i] == 0)
					continue;
				var position = (surface.Centers[i] - _center) / _extent;
				regionByRoot[i] = CloudPattern.FindRegion(
					new System.Numerics.Vector3(position.X, position.Y, position.Z), regions);
			}

			var vertices = (Vector3[])surface.Vertices.Clone();
			var normals = (Vector3[])surface.Normals.Clone();
			var tangents = (float[])surface.Tangents.Clone();
			for (var i = 0; i < vertices.Length; i++)
			{
				var regionIndex = regionByRoot[surface.Roots[i]];
				if (regionIndex < 0)
					continue;
				var (pivot, offset, rotation) = transforms[regionIndex];
				vertices[i] = pivot + offset + rotation * (vertices[i] - pivot);
				if (normals.Length == vertices.Length)
					normals[i] = rotation * normals[i];
				if (tangents.Length == vertices.Length * 4)
				{
					var tangent = rotation * new Vector3(tangents[i * 4], tangents[i * 4 + 1], tangents[i * 4 + 2]);
					tangents[i * 4] = tangent.X;
					tangents[i * 4 + 1] = tangent.Y;
					tangents[i * 4 + 2] = tangent.Z;
				}
			}

			var keptIndices = new List<int>();
			for (var i = 0; i < surface.Indices.Length; i += 3)
			{
				if (regionByRoot[surface.Roots[surface.Indices[i]]] < 0)
					continue;
				keptIndices.Add(surface.Indices[i]);
				keptIndices.Add(surface.Indices[i + 1]);
				keptIndices.Add(surface.Indices[i + 2]);
			}
			if (keptIndices.Count == 0)
				throw new InvalidOperationException("Cloud model has no pieces in the selected regions.");

			var arrays = surface.Arrays.Duplicate();
			arrays[(int)Mesh.ArrayType.Vertex] = vertices;
			arrays[(int)Mesh.ArrayType.Index] = keptIndices.ToArray();
			if (normals.Length == vertices.Length)
				arrays[(int)Mesh.ArrayType.Normal] = normals;
			if (tangents.Length == vertices.Length * 4)
				arrays[(int)Mesh.ArrayType.Tangent] = tangents;
			result.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		}
		return result;
	}

	private static Surface PrepareSurface(ArrayMesh mesh, int surface)
	{
		if (mesh.SurfaceGetPrimitiveType(surface) != Mesh.PrimitiveType.Triangles)
			throw new InvalidOperationException("Cloud model has a non-triangle surface.");
		var arrays = mesh.SurfaceGetArrays(surface);
		var vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
		var indices = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
		if (vertices.Length == 0 || indices.Length == 0 || indices.Length % 3 != 0)
			throw new InvalidOperationException("Cloud model has invalid geometry.");

		var parents = Enumerable.Range(0, vertices.Length).ToArray();
		int Root(int vertex)
		{
			while (parents[vertex] != vertex)
			{
				parents[vertex] = parents[parents[vertex]];
				vertex = parents[vertex];
			}
			return vertex;
		}

		for (var i = 0; i < indices.Length; i += 3)
		{
			var root = Root(indices[i]);
			parents[Root(indices[i + 1])] = root;
			parents[Root(indices[i + 2])] = root;
		}
		var roots = new int[vertices.Length];
		var centers = new Vector3[vertices.Length];
		var counts = new int[vertices.Length];
		for (var i = 0; i < vertices.Length; i++)
		{
			var root = Root(i);
			roots[i] = root;
			centers[root] += vertices[i];
			counts[root]++;
		}
		for (var i = 0; i < centers.Length; i++)
			if (counts[i] > 0)
				centers[i] /= counts[i];

		return new Surface(arrays, vertices, arrays[(int)Mesh.ArrayType.Normal].AsVector3Array(),
			arrays[(int)Mesh.ArrayType.Tangent].AsFloat32Array(), indices, roots, centers, counts);
	}

	private static Vector3 ToGodot(System.Numerics.Vector3 value) => new(value.X, value.Y, value.Z);
}
