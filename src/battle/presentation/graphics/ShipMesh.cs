using Godot;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public static class ShipMesh
{
	private const string FighterPath = "res://assets/models/ships/spaceship.glb";
	private const float FighterLength = 0.9f;
	private static PackedScene? _fighterModel;

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

	public static MeshInstance3D CreateFighterHull()
	{
		_fighterModel ??= GD.Load<PackedScene>(FighterPath)
			?? throw new InvalidOperationException($"Could not load fighter model '{FighterPath}'.");
		var scene = _fighterModel.Instantiate<Node3D>();

		try
		{
			var meshes = scene.FindChildren("*", "MeshInstance3D", true, false)
				.OfType<MeshInstance3D>().ToArray();
			if (meshes.Length == 0)
				throw new InvalidOperationException($"Fighter model '{FighterPath}' has no meshes.");

			var bounds = default(Aabb);
			for (var i = 0; i < meshes.Length; i++)
			{
				if (meshes[i].Mesh is not { } mesh)
					throw new InvalidOperationException($"Fighter model '{FighterPath}' has an empty mesh.");

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
				throw new InvalidOperationException($"Fighter model '{FighterPath}' has no forward extent.");

			var scale = FighterLength / bounds.Size.Z;
			var hull = new MeshInstance3D
			{
				Name = "FighterHull",
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
