using Godot;

using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class BoardHazardView : Node3D
{
	private const float CellFit = 0.94f;

	private static readonly Dictionary<string, string> MeshPaths = new()
	{
		["rock_small_a"] = "res://assets/models/asteroids/rock_small_a.glb",
		["rock_small_b"] = "res://assets/models/asteroids/rock_small_b.glb",
		["rock"] = "res://assets/models/asteroids/rock.glb",
		["rock_large_a"] = "res://assets/models/asteroids/rock_large_a.glb",
		["rock_large_b"] = "res://assets/models/asteroids/rock_large_b.glb",
	};
	private static readonly string[] Visuals = MeshPaths.Keys.ToArray();

	public void Build(IReadOnlyList<Hazard> hazards)
	{
		foreach (var hazard in hazards)
		{
			if (hazard.Kind == EHazardKind.Asteroid)
				AddChild(CreateAsteroid(hazard));
		}
	}

	private static Node3D CreateAsteroid(Hazard hazard)
	{
		var root = new Node3D { Name = hazard.Id };
		foreach (var cell in hazard.Cells)
			root.AddChild(CreateCellRock(cell));

		ApplyRockMaterial(root, RngFor(hazard.Center));
		return root;
	}

	private static Node3D CreateCellRock(Coord cell)
	{
		var rng = RngFor(cell);
		var visualId = Visuals[rng.RandiRange(0, Visuals.Length - 1)];
		var packed = GD.Load<PackedScene>(MeshPaths[visualId]);
		var instance = packed.Instantiate<Node3D>();
		var bounds = BoundsOf(instance);
		var center = (bounds.Min + bounds.Max) * 0.5f;
		var radius = FurthestCornerDistance(bounds, center);
		if (radius <= 0f)
			throw new InvalidOperationException($"Asteroid visual '{visualId}' has no renderable bounds.");
		var scale = WorldMapping.CellSize * CellFit * 0.5f / radius;

		var cellRoot = new Node3D { Position = WorldMapping.ToWorld(cell) };
		var rotation = new Node3D
		{
			RotationDegrees = new Vector3(
				rng.RandfRange(0f, 360f),
				rng.RandfRange(0f, 360f),
				rng.RandfRange(0f, 360f)),
		};
		var sizing = new Node3D
		{
			Scale = new Vector3(
				scale * rng.RandfRange(0.78f, 1f),
				scale * rng.RandfRange(0.78f, 1f),
				scale * rng.RandfRange(0.78f, 1f)),
		};
		var centering = new Node3D { Position = -center };

		cellRoot.AddChild(rotation);
		rotation.AddChild(sizing);
		sizing.AddChild(centering);
		centering.AddChild(instance);
		return cellRoot;
	}

	private static (Vector3 Min, Vector3 Max) BoundsOf(Node3D root)
	{
		var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
		var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
		CollectBounds(root, Transform3D.Identity, ref min, ref max);
		return float.IsPositiveInfinity(min.X)
			? (Vector3.Zero, Vector3.Zero)
			: (min, max);
	}

	private static void CollectBounds(
		Node node,
		Transform3D parentTransform,
		ref Vector3 min,
		ref Vector3 max)
	{
		var transform = node is Node3D node3D
			? parentTransform * node3D.Transform
			: parentTransform;

		if (node is MeshInstance3D { Mesh: not null } mesh)
		{
			var bounds = mesh.GetAabb();
			for (var x = 0; x <= 1; x++)
			{
				for (var y = 0; y <= 1; y++)
				{
					for (var z = 0; z <= 1; z++)
					{
						var corner = bounds.Position + new Vector3(
							x * bounds.Size.X,
							y * bounds.Size.Y,
							z * bounds.Size.Z);
						var transformed = transform * corner;
						min = Min(min, transformed);
						max = Max(max, transformed);
					}
				}
			}
		}

		foreach (var child in node.GetChildren())
			CollectBounds(child, transform, ref min, ref max);
	}

	private static float FurthestCornerDistance((Vector3 Min, Vector3 Max) bounds, Vector3 center)
	{
		var maxDistance = 0f;
		for (var x = 0; x <= 1; x++)
		{
			for (var y = 0; y <= 1; y++)
			{
				for (var z = 0; z <= 1; z++)
				{
					var corner = new Vector3(
						x == 0 ? bounds.Min.X : bounds.Max.X,
						y == 0 ? bounds.Min.Y : bounds.Max.Y,
						z == 0 ? bounds.Min.Z : bounds.Max.Z);
					maxDistance = Mathf.Max(maxDistance, center.DistanceTo(corner));
				}
			}
		}

		return maxDistance;
	}

	private static Vector3 Min(Vector3 a, Vector3 b) =>
		new(Mathf.Min(a.X, b.X), Mathf.Min(a.Y, b.Y), Mathf.Min(a.Z, b.Z));

	private static Vector3 Max(Vector3 a, Vector3 b) =>
		new(Mathf.Max(a.X, b.X), Mathf.Max(a.Y, b.Y), Mathf.Max(a.Z, b.Z));

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
