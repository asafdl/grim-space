using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

public static class PatrolMesh
{
	private const string ModelPath = "res://assets/models/ships/low_poly_spaceship_110.glb";
	private const float Length = 1.25f;
	private static Mesh? _mesh;
	private static Aabb _bounds;
	private static ArrayMesh? _previewMesh;

	public static MeshInstance3D CreateHullInstance()
	{
		EnsureLoaded();
		var scale = Length / _bounds.Size.Z;
		return new MeshInstance3D
		{
			Name = "PatrolHull",
			Mesh = _mesh,
			Position = -_bounds.GetCenter() * scale,
			Scale = Vector3.One * scale,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
	}

	public static Mesh CreatePreviewHull()
	{
		EnsureLoaded();
		if (_previewMesh is not null)
			return _previewMesh;

		var arrays = _mesh!.SurfaceGetArrays(0);
		var vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
		var scale = Length / _bounds.Size.Z;
		for (var i = 0; i < vertices.Length; i++)
			vertices[i] = (vertices[i] - _bounds.GetCenter()) * scale;
		arrays[(int)Mesh.ArrayType.Vertex] = vertices;

		var preview = new ArrayMesh();
		preview.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		_previewMesh = preview;
		return preview;
	}

	private static void EnsureLoaded()
	{
		if (_mesh is not null)
			return;

		var scene = GD.Load<PackedScene>(ModelPath)
			?? throw new InvalidOperationException($"Could not load patrol model '{ModelPath}'.");
		var root = scene.Instantiate<Node3D>();
		try
		{
			var meshes = root.FindChildren("*", "MeshInstance3D", true, false)
				.OfType<MeshInstance3D>().ToArray();
			if (meshes.Length != 1 || meshes[0].Mesh is not { } mesh || mesh.GetSurfaceCount() != 1)
				throw new InvalidOperationException($"Patrol model '{ModelPath}' must contain one mesh surface.");

			_bounds = mesh.GetAabb();
			if (_bounds.Size.Z <= 0f)
				throw new InvalidOperationException($"Patrol model '{ModelPath}' has no forward extent.");
			_mesh = mesh;
		}
		finally
		{
			root.Free();
		}
	}
}
