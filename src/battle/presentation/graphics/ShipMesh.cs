using Godot;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public static class ShipMesh
{
	private const string FighterPath = "res://assets/models/ships/spaceship_colaid1_50k.glb";
	private const float FighterLength = 1.8f;
	private static Mesh? _fighterMesh;
	private static Aabb _fighterBounds;

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
		if (_fighterMesh is null)
		{
			var scene = GD.Load<PackedScene>(FighterPath)
				?? throw new InvalidOperationException($"Could not load fighter model '{FighterPath}'.");
			var root = scene.Instantiate<Node3D>();
			try
			{
				var meshes = root.FindChildren("*", "MeshInstance3D", true, false)
					.OfType<MeshInstance3D>().ToArray();
				if (meshes.Length != 1 || meshes[0].Mesh is not { } mesh)
					throw new InvalidOperationException($"Fighter model '{FighterPath}' must contain one mesh.");

				_fighterBounds = mesh.GetAabb();
				if (_fighterBounds.Size.Z <= 0f)
					throw new InvalidOperationException($"Fighter model '{FighterPath}' has no forward extent.");
				_fighterMesh = mesh;
			}
			finally
			{
				root.Free();
			}
		}

		var scale = FighterLength / _fighterBounds.Size.Z;
		return new MeshInstance3D
		{
			Name = "FighterHull",
			Mesh = _fighterMesh,
			Scale = Vector3.One * scale,
			Position = -_fighterBounds.GetCenter() * scale,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
	}
}
