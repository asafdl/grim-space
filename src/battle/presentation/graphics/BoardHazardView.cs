using Godot;
using GrimSpace.Battle.Board;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class BoardHazardView : Node3D
{
	private static readonly Dictionary<string, string> MeshPaths = new()
	{
		["rock_small_a"] = "res://assets/models/asteroids/rock_small_a.glb",
		["rock_small_b"] = "res://assets/models/asteroids/rock_small_b.glb",
		["rock"] = "res://assets/models/asteroids/rock.glb",
		["rock_large_a"] = "res://assets/models/asteroids/rock_large_a.glb",
		["rock_large_b"] = "res://assets/models/asteroids/rock_large_b.glb",
	};

	public void Build(IReadOnlyList<Hazard> hazards)
	{
		foreach (var hazard in hazards)
			AddChild(CreateInstance(hazard));
	}

	private static Node3D CreateInstance(Hazard hazard)
	{
		var visualId = hazard.VisualId ?? "rock_large_a";
		var path = MeshPaths.GetValueOrDefault(visualId, MeshPaths["rock_large_a"]);
		var packed = GD.Load<PackedScene>(path);
		var instance = packed.Instantiate<Node3D>();
		var rng = RngFor(hazard.Center);

		instance.Position = WorldMapping.ToWorld(hazard.Center);
		instance.Scale = AsymmetricScale(hazard.Radius, rng);
		instance.RotationDegrees = new Vector3(
			rng.RandfRange(0f, 360f),
			rng.RandfRange(0f, 360f),
			rng.RandfRange(0f, 360f));
		ApplyRockMaterial(instance, rng);

		return instance;
	}

	private static Vector3 AsymmetricScale(int radius, RandomNumberGenerator rng)
	{
		var footprint = (radius * 2 + 1) * WorldMapping.CellSize;
		var baseScale = footprint * 0.52f;

		return new Vector3(
			baseScale * rng.RandfRange(0.82f, 1.18f),
			baseScale * rng.RandfRange(0.62f, 1.28f),
			baseScale * rng.RandfRange(0.78f, 1.22f));
	}

	private static void ApplyRockMaterial(Node3D root, RandomNumberGenerator rng)
	{
		var tint = rng.Randf();
		var albedo = tint switch
		{
			< 0.33f => new Color(0.38f, 0.34f, 0.3f),
			< 0.66f => new Color(0.32f, 0.3f, 0.28f),
			_ => new Color(0.42f, 0.36f, 0.32f),
		};

		var material = new StandardMaterial3D
		{
			AlbedoColor = albedo,
			Roughness = rng.RandfRange(0.82f, 0.98f),
			Metallic = rng.RandfRange(0.02f, 0.08f),
		};

		foreach (var mesh in root.FindChildren("*", "MeshInstance3D", recursive: true, owned: false))
		{
			if (mesh is MeshInstance3D instance)
			{
				instance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
				instance.MaterialOverride = material;
			}
		}
	}

	private static RandomNumberGenerator RngFor(Coord center)
	{
		var rng = new RandomNumberGenerator();
		rng.Seed = (ulong)(center.X * 73856093 ^ center.Y * 19349663 ^ center.Z * 83492791);
		return rng;
	}
}
