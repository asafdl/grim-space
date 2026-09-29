using System;
using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

internal static class ScrapDroneMesh
{
	private const string ModelPath = "res://assets/vfx/mech_drone.glb";
	private const float MountPreviewSize = 0.63f;

	private static PackedScene? _model;

	public static MeshInstance3D CreateMountPreview()
	{
		_model ??= GD.Load<PackedScene>(ModelPath)
			?? throw new InvalidOperationException($"Could not load scrap drone model '{ModelPath}'.");

		var scene = _model.Instantiate<Node3D>();
		try
		{
			var meshes = scene.FindChildren("*", "MeshInstance3D", true, false);
			if (meshes.Count == 0)
				throw new InvalidOperationException($"Scrap drone model '{ModelPath}' has no meshes.");

			var bounds = default(Aabb);
			var hasBounds = false;
			foreach (var node in meshes)
			{
				if (node is not MeshInstance3D mesh || mesh.Mesh is not { } surface)
					continue;

				var transform = Transform3D.Identity;
				for (Node3D? current = mesh; current is not null; current = current.GetParent() as Node3D)
				{
					transform = current.Transform * transform;
					if (current == scene)
						break;
				}

				var partBounds = transform * surface.GetAabb();
				bounds = hasBounds ? bounds.Merge(partBounds) : partBounds;
				hasBounds = true;
				mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
			}

			if (!hasBounds)
				throw new InvalidOperationException($"Scrap drone model '{ModelPath}' has no mesh bounds.");

			var extent = System.Math.Max(
				bounds.Size.X,
				System.Math.Max(bounds.Size.Y, bounds.Size.Z));
			if (extent <= 0f)
				throw new InvalidOperationException($"Scrap drone model '{ModelPath}' has no extent.");

			var scale = MountPreviewSize / extent;
			var hull = new MeshInstance3D
			{
				Name = "ScrapDroneMountGhost",
				Position = -bounds.GetCenter() * scale,
				Scale = Vector3.One * scale,
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			};
			hull.AddChild(scene);
			return hull;
		}
		catch
		{
			if (scene.GetParent() is null)
				scene.Free();
			throw;
		}
	}
}
