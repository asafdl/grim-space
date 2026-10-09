using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

public static class IndustrialGooperMesh
{
	private const string ModelPath = "res://assets/models/ships/gooper.glb";
	private const float Length = 0.7f;
	private static PackedScene? _model;

	public static MeshInstance3D CreateHullInstance()
	{
		_model ??= GD.Load<PackedScene>(ModelPath)
			?? throw new InvalidOperationException($"Could not load industrial gooper model '{ModelPath}'.");
		var scene = _model.Instantiate<Node3D>();

		try
		{
			var meshes = scene.FindChildren("*", "MeshInstance3D", true, false)
				.OfType<MeshInstance3D>().ToArray();
			if (meshes.Length == 0)
				throw new InvalidOperationException($"Industrial gooper model '{ModelPath}' has no meshes.");

			var bounds = default(Aabb);
			for (var i = 0; i < meshes.Length; i++)
			{
				if (meshes[i].Mesh is not { } mesh)
					throw new InvalidOperationException($"Industrial gooper model '{ModelPath}' has an empty mesh.");

				var transform = Transform3D.Identity;
				for (Node3D? node = meshes[i]; node is not null; node = node.GetParent() as Node3D)
				{
					transform = node.Transform * transform;
					if (node == scene)
						break;
				}

				var partBounds = transform * mesh.GetAabb();
				bounds = i == 0 ? partBounds : bounds.Merge(partBounds);
				meshes[i].CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
			}

			if (bounds.Size.Z <= 0f)
				throw new InvalidOperationException($"Industrial gooper model '{ModelPath}' has no forward extent.");

			var scale = Length / bounds.Size.Z;
			var hull = new MeshInstance3D
			{
				Name = "IndustrialGooperHull",
				Position = -bounds.GetCenter() * scale,
				Scale = Vector3.One * scale,
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			};
			hull.AddChild(scene);
			return hull;
		}
		finally
		{
			if (scene.GetParent() is null)
				scene.Free();
		}
	}
}
