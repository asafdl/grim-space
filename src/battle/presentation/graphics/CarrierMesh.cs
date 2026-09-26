using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

public static class CarrierMesh
{
	private const string ModelPath = "res://assets/models/ships/cool_spaceship_with_logo.glb";
	private const float Length = 2.5f;
	private static Mesh[]? _parts;
	private static Aabb _bounds;

	public static MeshInstance3D CreateHullInstance()
	{
		EnsureLoaded();
		var scale = Length / _bounds.Size.Z;
		var hull = new MeshInstance3D
		{
			Name = "CarrierHull",
			Mesh = _parts![0],
			Scale = Vector3.One * scale,
			Position = -_bounds.GetCenter() * scale,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		for (var i = 1; i < _parts.Length; i++)
			hull.AddChild(new MeshInstance3D
			{
				Name = $"CarrierPart_{i}",
				Mesh = _parts[i],
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			});
		return hull;
	}

	private static void EnsureLoaded()
	{
		if (_parts is not null)
			return;

		var scene = GD.Load<PackedScene>(ModelPath)
			?? throw new InvalidOperationException($"Could not load carrier model '{ModelPath}'.");
		var root = scene.Instantiate<Node3D>();
		try
		{
			var meshes = root.FindChildren("*", "MeshInstance3D", true, false)
				.OfType<MeshInstance3D>()
				.OrderBy(part => part.Name.ToString(), StringComparer.Ordinal)
				.ToArray();
			if (meshes.Length == 0)
				throw new InvalidOperationException($"Carrier model '{ModelPath}' has no mesh parts.");

			var parts = new Mesh[meshes.Length];
			var bounds = default(Aabb);
			for (var i = 0; i < meshes.Length; i++)
			{
				if (meshes[i].Mesh is not { } mesh || mesh.GetSurfaceCount() != 1
					|| mesh.SurfaceGetMaterial(0) is null)
					throw new InvalidOperationException($"Carrier model '{ModelPath}' has an invalid mesh part '{meshes[i].Name}'.");

				parts[i] = mesh;
				bounds = i == 0 ? mesh.GetAabb() : bounds.Merge(mesh.GetAabb());
			}

			if (bounds.Size.Z <= 0f)
				throw new InvalidOperationException($"Carrier model '{ModelPath}' has no forward extent.");
			_bounds = bounds;
			_parts = parts;
		}
		finally
		{
			root.Free();
		}
	}
}
