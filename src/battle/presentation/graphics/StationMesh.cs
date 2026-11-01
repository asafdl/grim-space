using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>
/// The storage station baked into a normalized frame: centered on the origin, longest axis
/// along Z with length 1. Surface 0 is the hull; subsequent surfaces retain each source light
/// material's texture mapping. Built once per process.
/// </summary>
public static class StationMesh
{
	public const int HullSurface = 0;

	private const string ModelPath = "res://assets/models/spaceobjects/freeport_space_station1.glb";

	private static ArrayMesh? _mesh;
	private static BaseMaterial3D? _hullMaterial;
	private static readonly List<BaseMaterial3D> _lightMaterials = [];
	private static Vector3 _size;

	public static ArrayMesh Hull
	{
		get
		{
			EnsureBuilt();
			return _mesh!;
		}
	}

	public static BaseMaterial3D HullMaterial
	{
		get
		{
			EnsureBuilt();
			return _hullMaterial!;
		}
	}

	public static IReadOnlyList<BaseMaterial3D> LightMaterials
	{
		get
		{
			EnsureBuilt();
			return _lightMaterials;
		}
	}

	/// <summary>Bounding size in the normalized frame; Z is always 1.</summary>
	public static Vector3 Size
	{
		get
		{
			EnsureBuilt();
			return _size;
		}
	}

	private sealed record Surface(Vector3[] Positions, Vector3[] Normals, Vector2[] Uvs, int[] Indices);

	private static void EnsureBuilt()
	{
		if (_mesh is not null)
			return;

		var scene = GD.Load<PackedScene>(ModelPath)
			?? throw new InvalidOperationException($"Could not load station model '{ModelPath}'.");
		var root = scene.Instantiate<Node3D>();
		try
		{
			var hull = new List<Surface>();
			var lights = new List<List<Surface>>();
			_lightMaterials.Clear();
			_hullMaterial = Collect(root, hull, lights)
				?? throw new InvalidOperationException($"Station model '{ModelPath}' has no textured, non-emissive surfaces.");
			if (lights.Count == 0)
				throw new InvalidOperationException($"Station model '{ModelPath}' has no emissive window or light surfaces.");

			_mesh = Bake(hull, lights);
		}
		finally
		{
			root.Free();
		}
	}

	private static BaseMaterial3D? Collect(Node3D root, List<Surface> hull, List<List<Surface>> lights)
	{
		BaseMaterial3D? hullMaterial = null;
		foreach (var part in root.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
		{
			if (part.Mesh is not { } mesh)
				continue;

			var transform = Transform3D.Identity;
			for (Node? node = part; node is not null && node != root; node = node.GetParent())
			{
				if (node is Node3D spatial)
					transform = spatial.Transform * transform;
			}

			var normalBasis = transform.Basis.Inverse().Transposed();
			for (var surface = 0; surface < mesh.GetSurfaceCount(); surface++)
			{
				if (mesh.SurfaceGetMaterial(surface) is not BaseMaterial3D material)
					continue;

				if (material.EmissionEnabled)
				{
					var index = _lightMaterials.IndexOf(material);
					if (index < 0)
					{
						index = _lightMaterials.Count;
						_lightMaterials.Add(material);
						lights.Add([]);
					}
					lights[index].Add(Read(mesh, surface, transform, normalBasis));
					continue;
				}

				if (material.AlbedoTexture is not { } texture)
					continue;

				hullMaterial ??= material;
				if (texture == hullMaterial.AlbedoTexture)
					hull.Add(Read(mesh, surface, transform, normalBasis));
			}
		}

		return hullMaterial;
	}

	private static Surface Read(Mesh mesh, int surface, Transform3D transform, Basis normalBasis)
	{
		var arrays = mesh.SurfaceGetArrays(surface);
		var positions = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
		var indexVariant = arrays[(int)Mesh.ArrayType.Index];
		return new Surface(
			[.. positions.Select(position => transform * position)],
			[.. arrays[(int)Mesh.ArrayType.Normal].AsVector3Array()
				.Select(normal => (normalBasis * normal).Normalized())],
			arrays[(int)Mesh.ArrayType.TexUV].AsVector2Array(),
			indexVariant.VariantType == Variant.Type.Nil
				? Enumerable.Range(0, positions.Length).ToArray()
				: indexVariant.AsInt32Array());
	}

	private static ArrayMesh Bake(List<Surface> hull, List<List<Surface>> lights)
	{
		var all = hull.Concat(lights.SelectMany(surfaces => surfaces)).ToList();
		var bounds = Bounds(all, Basis.Identity);
		var rotation = (int)bounds.GetLongestAxisIndex() switch
		{
			0 => new Basis(Vector3.Up, Mathf.Pi * 0.5f),
			1 => new Basis(Vector3.Right, Mathf.Pi * 0.5f),
			_ => Basis.Identity,
		};
		bounds = Bounds(all, rotation);
		var scale = 1f / bounds.Size.Z;
		var center = bounds.GetCenter();
		_size = bounds.Size * scale;

		var mesh = new ArrayMesh();
		AddSurface(mesh, hull, rotation, center, scale);
		foreach (var surfaces in lights)
			AddSurface(mesh, surfaces, rotation, center, scale);
		return mesh;
	}

	private static void AddSurface(ArrayMesh mesh, List<Surface> surfaces, Basis rotation, Vector3 center, float scale)
	{
		var vertices = new List<Vector3>();
		var normals = new List<Vector3>();
		var uvs = new List<Vector2>();
		var indices = new List<int>();
		foreach (var surface in surfaces)
		{
			var offset = vertices.Count;
			vertices.AddRange(surface.Positions.Select(position => (rotation * position - center) * scale));
			normals.AddRange(surface.Normals.Select(normal => rotation * normal));
			uvs.AddRange(surface.Uvs);
			indices.AddRange(surface.Indices.Select(index => index + offset));
		}

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
		arrays[(int)Mesh.ArrayType.Normal] = normals.ToArray();
		arrays[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
		arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
	}

	private static Aabb Bounds(List<Surface> surfaces, Basis rotation)
	{
		var first = true;
		var min = Vector3.Zero;
		var max = Vector3.Zero;
		foreach (var position in surfaces.SelectMany(surface => surface.Positions))
		{
			var rotated = rotation * position;
			min = first ? rotated : min.Min(rotated);
			max = first ? rotated : max.Max(rotated);
			first = false;
		}

		return new Aabb(min, max - min);
	}
}
