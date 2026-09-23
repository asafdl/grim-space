using Godot;

namespace GrimSpace.World.StarSystem.Presentation.Map;

internal static class NavigationLandmarkRockLibrary
{
	private static readonly string[] MainRockScenePaths =
	[
		"res://assets/models/asteroids/rock.glb",
		"res://assets/models/asteroids/rock_large_a.glb",
		"res://assets/models/asteroids/rock_large_b.glb",
	];
	private static readonly string[] SmallRockScenePaths =
	[
		"res://assets/models/asteroids/rock_small_a.glb",
		"res://assets/models/asteroids/rock_small_b.glb",
	];

	private static PackedScene[]? _mainScenes;
	private static PackedScene[]? _smallScenes;

	public static Node3D CreateRock(
		RandomNumberGenerator random,
		float targetDiameter,
		bool mainMass)
	{
		EnsureLoaded();
		var scenes = mainMass ? _mainScenes! : _smallScenes!;
		var scene = scenes[random.Randi() % scenes.Length];
		var meshRoot = scene.Instantiate<Node3D>();
		var bounds = MeasureLocalBounds(meshRoot);
		var maxExtent = Mathf.Max(bounds.Size.X, Mathf.Max(bounds.Size.Y, bounds.Size.Z));
		var uniformScale = targetDiameter / Mathf.Max(maxExtent, 0.001f);

		var pivot = new Node3D { Name = mainMass ? "MainRock" : "Rock" };
		meshRoot.Position = -bounds.GetCenter();
		pivot.AddChild(meshRoot);
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
		if (_mainScenes is not null)
			return;

		_mainScenes = MainRockScenePaths.Select(GD.Load<PackedScene>).ToArray();
		_smallScenes = SmallRockScenePaths.Select(GD.Load<PackedScene>).ToArray();
	}

	private static Aabb MeasureLocalBounds(Node root)
	{
		var combined = new Aabb();
		var hasBounds = false;

		foreach (var node in root.FindChildren("*", "VisualInstance3D", true, false))
		{
			if (node is not VisualInstance3D visual)
				continue;

			var local = visual.GetAabb();
			if (local.Size == Vector3.Zero)
				continue;

			combined = hasBounds ? combined.Merge(local) : local;
			hasBounds = true;
		}

		return hasBounds ? combined : new Aabb(Vector3.Zero, Vector3.One * 0.1f);
	}
}
