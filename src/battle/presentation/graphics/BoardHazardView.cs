using Godot;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class BoardHazardView : Node3D
{
	private const string PackPath = "res://assets/models/asteroids/asteroids_pack_metallic_version.glb";
	private const string MetallicPackPath = "res://assets/models/asteroids/asteroids.glb";
	private const float CellFit = 0.96f;
	private static RockVariant[]? _rocks;
	private static RockVariant[]? _metallicRocks;

	private static readonly Color[] RockBases =
	[
		new(0.76f, 0.52f, 0.32f),
		new(0.57f, 0.42f, 0.34f),
		new(0.68f, 0.61f, 0.38f),
		new(0.39f, 0.57f, 0.52f),
		new(0.42f, 0.51f, 0.68f),
		new(0.58f, 0.49f, 0.57f),
	];
	private static readonly Color[] MetallicBases =
	[
		new(0.76f, 0.81f, 0.87f),
		new(0.86f, 0.71f, 0.44f),
		new(0.58f, 0.74f, 0.76f),
	];

	public void Build(IReadOnlyList<Hazard> hazards)
	{
		if (hazards.Any(hazard => hazard.Kind == EHazardKind.Asteroid))
			EnsureLoaded();

		foreach (var hazard in hazards)
		{
			if (hazard.Kind == EHazardKind.Asteroid)
				AddChild(CreateAsteroid(hazard));
		}
	}

	private static MeshInstance3D CreateAsteroid(Hazard hazard)
	{
		var rng = RngFor(hazard.Center);
		var metallic = rng.Randf() < 0.18f;
		var variants = metallic ? _metallicRocks! : _rocks!;
		var variant = variants[rng.Randi() % variants.Length];
		var minX = hazard.Cells.Min(cell => cell.X);
		var minY = hazard.Cells.Min(cell => cell.Y);
		var minZ = hazard.Cells.Min(cell => cell.Z);
		var maxX = hazard.Cells.Max(cell => cell.X);
		var maxY = hazard.Cells.Max(cell => cell.Y);
		var maxZ = hazard.Cells.Max(cell => cell.Z);
		var center = (WorldMapping.ToWorld(new Coord(minX, minY, minZ))
			+ WorldMapping.ToWorld(new Coord(maxX, maxY, maxZ))) * 0.5f;
		var availableSize = new Vector3(
			maxX - minX + 1, maxY - minY + 1, maxZ - minZ + 1) * (WorldMapping.CellSize * CellFit);
		var rotation = Basis.FromEuler(new Vector3(
			rng.Randf() * Mathf.Tau,
			rng.Randf() * Mathf.Tau,
			rng.Randf() * Mathf.Tau));
		var rotatedSize = rotation.X.Abs() * variant.Bounds.Size.X
			+ rotation.Y.Abs() * variant.Bounds.Size.Y
			+ rotation.Z.Abs() * variant.Bounds.Size.Z;
		var scale = Mathf.Min(
			availableSize.X / rotatedSize.X,
			Mathf.Min(availableSize.Y / rotatedSize.Y, availableSize.Z / rotatedSize.Z));
		var palette = metallic ? MetallicBases : RockBases;
		var color = palette[rng.RandiRange(0, palette.Length - 1)];

		return new MeshInstance3D
		{
			Name = hazard.Id,
			Position = center - rotation * (variant.Bounds.GetCenter() * scale),
			Basis = rotation.ScaledLocal(Vector3.One * scale),
			Mesh = variant.Mesh,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = CreateRockMaterial(variant.Mesh, color, rng, metallic),
		};
	}

	private static StandardMaterial3D CreateRockMaterial(
		Mesh mesh,
		Color color,
		RandomNumberGenerator rng,
		bool metallic)
	{
		if (mesh.SurfaceGetMaterial(0) is not StandardMaterial3D source)
			throw new InvalidOperationException("Battle asteroid mesh has no standard material.");

		var material = (StandardMaterial3D)source.Duplicate();
		material.AlbedoColor *= Colors.White.Lerp(color, metallic ? 0.5f : 0.65f)
			* rng.RandfRange(0.85f, 1.12f);
		material.Roughness = metallic
			? rng.RandfRange(0.28f, 0.4f)
			: Mathf.Max(material.Roughness, 0.8f);
		material.Metallic = metallic
			? rng.RandfRange(0.65f, 0.8f)
			: Mathf.Min(material.Metallic, 0.18f);
		return material;
	}

	private static void EnsureLoaded()
	{
		if (_rocks is not null && _metallicRocks is not null)
			return;

		_rocks = LoadRocks(PackPath);
		_metallicRocks = LoadRocks(MetallicPackPath)
			.Where(rock => rock.Mesh is ArrayMesh mesh && mesh.SurfaceGetArrayLen(0) <= 500)
			.ToArray();
		if (_metallicRocks.Length == 0)
			throw new InvalidOperationException($"Asteroid pack '{MetallicPackPath}' has no low-detail meshes.");
	}

	private static RockVariant[] LoadRocks(string path)
	{
		var scene = GD.Load<PackedScene>(path)
			?? throw new InvalidOperationException($"Could not load asteroid pack '{path}'.");
		var root = scene.Instantiate<Node3D>();
		try
		{
			var rocks = root.FindChildren("*", "MeshInstance3D", true, false)
				.OfType<MeshInstance3D>()
				.OrderBy(node => node.Name.ToString(), StringComparer.Ordinal)
				.Select(node => node.Mesh is { } mesh
					? new RockVariant(mesh, mesh.GetAabb())
					: throw new InvalidOperationException($"Asteroid '{node.Name}' has no mesh."))
				.ToArray();
			if (rocks.Length == 0)
				throw new InvalidOperationException($"Asteroid pack '{path}' has no meshes.");
			return rocks;
		}
		finally
		{
			root.Free();
		}
	}

	private static RandomNumberGenerator RngFor(Coord center)
	{
		var rng = new RandomNumberGenerator();
		rng.Seed = (ulong)(center.X * 73856093 ^ center.Y * 19349663 ^ center.Z * 83492791);
		return rng;
	}

	private readonly record struct RockVariant(Mesh Mesh, Aabb Bounds);
}
