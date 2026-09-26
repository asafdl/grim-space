using Godot;

namespace GrimSpace.World.StarSystem.Presentation.Map;

internal static class NavigationLandmarkRockLibrary
{
	private const string PackPath = "res://assets/models/asteroids/asteroids_pack_metallic_version.glb";
	private static (Mesh Mesh, Aabb Bounds)[]? _rocks;

	public static Node3D CreateRock(
		RandomNumberGenerator random,
		float targetDiameter,
		bool mainMass)
	{
		EnsureLoaded();
		var (mesh, bounds) = _rocks![random.Randi() % _rocks.Length];
		var maxExtent = Mathf.Max(bounds.Size.X, Mathf.Max(bounds.Size.Y, bounds.Size.Z));
		var uniformScale = targetDiameter / Mathf.Max(maxExtent, 0.001f);

		var pivot = new Node3D { Name = mainMass ? "MainRock" : "Rock" };
		pivot.AddChild(new MeshInstance3D
		{
			Mesh = mesh,
			Position = -bounds.GetCenter(),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		});
		pivot.Scale = Vector3.One * uniformScale;
		return pivot;
	}

	public static void TintMeshes(
		Node3D root,
		Color accent,
		float roughness = 0.88f,
		float emissionStrength = 0.07f)
	{
		var albedo = Colors.White.Lerp(accent, 0.68f);
		foreach (var node in root.FindChildren("*", "MeshInstance3D", true, false))
		{
			if (node is not MeshInstance3D mesh)
				continue;

			var source = mesh.MaterialOverride as StandardMaterial3D
				?? mesh.GetActiveMaterial(0) as StandardMaterial3D;
			if (source is null)
				continue;

			var copy = (StandardMaterial3D)source.Duplicate();
			copy.AlbedoColor *= albedo;
			copy.Roughness = roughness;
			copy.Metallic = Mathf.Min(copy.Metallic, 0.22f);
			if (emissionStrength > 0f)
			{
				copy.EmissionEnabled = true;
				copy.Emission = accent * emissionStrength;
				copy.EmissionEnergyMultiplier = 1.15f;
			}

			mesh.MaterialOverride = copy;
		}
	}

	private static void EnsureLoaded()
	{
		if (_rocks is not null)
			return;

		var scene = GD.Load<PackedScene>(PackPath)
			?? throw new InvalidOperationException($"Could not load asteroid pack '{PackPath}'.");
		var root = scene.Instantiate<Node3D>();
		try
		{
			var rocks = root.FindChildren("*", "MeshInstance3D", true, false)
				.OfType<MeshInstance3D>()
				.OrderBy(node => node.Name.ToString(), StringComparer.Ordinal)
				.Select(node => node.Mesh is { } mesh
					? (Mesh: mesh, Bounds: mesh.GetAabb())
					: throw new InvalidOperationException($"Asteroid '{node.Name}' has no mesh."))
				.ToArray();
			if (rocks.Length == 0)
				throw new InvalidOperationException($"Asteroid pack '{PackPath}' has no meshes.");
			_rocks = rocks;
		}
		finally
		{
			root.Free();
		}
	}
}
