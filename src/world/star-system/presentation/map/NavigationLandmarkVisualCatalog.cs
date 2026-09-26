using Godot;
using GrimSpace.Math;
using GrimSpace.World.StarSystem.Landmarks;
using GrimSpace.World.StarSystem.Presentation.Atmosphere;
using GrimSpace.World.StarSystem.Presentation.Picking;

namespace GrimSpace.World.StarSystem.Presentation.Map;

internal static class NavigationLandmarkVisualCatalog
{
	public static Node3D Build(NavigationLandmark landmark)
	{
		var root = new Node3D { Name = landmark.Id };
		var random = CreateRng(landmark.VisualSeed);
		var worldRadius = MapMapping.ToWorldRadius(landmark.Radius);

		switch (landmark.Kind)
		{
			case ENavigationLandmarkKind.DustCloud:
				NavigationLandmarkDustVisuals.AddCloud(root, worldRadius, random, landmark.VisualSeed);
				break;
			case ENavigationLandmarkKind.TailingsField:
				AddTailingsField(root, worldRadius, random, landmark.VisualSeed);
				break;
			case ENavigationLandmarkKind.AsteroidFormation:
				AddAsteroidFormation(root, worldRadius, random);
				break;
			case ENavigationLandmarkKind.Moonlet:
				MapCelestialVisuals.AddMoonlet(root, landmark.VisualSeed, worldRadius);
				break;
			case ENavigationLandmarkKind.SurveyRelay:
				AddSurveyRelayFallback(root, worldRadius, random);
				break;
			case ENavigationLandmarkKind.DebrisField:
				AddDebrisFallback(root, worldRadius, random);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(landmark.Kind), landmark.Kind, null);
		}

		return root;
	}

	public static Aabb MeasureLocalBounds(Node3D root)
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

		return hasBounds ? combined : new Aabb(Vector3.Zero, Vector3.One * 0.05f);
	}

	private static RandomNumberGenerator CreateRng(int visualSeed)
	{
		var rng = new RandomNumberGenerator();
		rng.Seed = (ulong)StableSeedMixer.From(visualSeed).Add("nav-landmark-visual").Value;
		return rng;
	}

	private static void AddAsteroidFormation(Node3D root, float worldRadius, RandomNumberGenerator random)
	{
		var layout = (int)(random.Randi() % 3);
		var mainCount = 3 + (int)(random.Randi() % 3);
		var supportCount = 8 + (int)(random.Randi() % 9);
		var clusterRadius = worldRadius * 0.7f;

		for (var i = 0; i < mainCount; i++)
		{
			var position = LayoutPoint(layout, i, mainCount, clusterRadius, random, 0.55f);
			var diameter = worldRadius * (0.3f + random.Randf() * 0.35f);
			var rock = NavigationLandmarkRockLibrary.CreateRock(random, diameter, mainMass: true);
			rock.Position = position;
			rock.RotationDegrees = new Vector3(
				random.Randf() * 35f,
				random.Randf() * 360f,
				random.Randf() * 35f);
			NavigationLandmarkRockLibrary.TintMeshes(
				rock,
				NavigationLandmarkPalette.MainRockAccent(random),
				emissionStrength: 0.09f);
			root.AddChild(rock);
		}

		for (var i = 0; i < supportCount; i++)
		{
			var position = LayoutPoint(layout, i, supportCount, clusterRadius, random, 0.95f);
			var diameter = worldRadius * (0.08f + random.Randf() * 0.12f);
			var rock = NavigationLandmarkRockLibrary.CreateRock(random, diameter, mainMass: false);
			rock.Position = position;
			rock.RotationDegrees = new Vector3(
				random.Randf() * 180f,
				random.Randf() * 360f,
				random.Randf() * 180f);
			NavigationLandmarkRockLibrary.TintMeshes(
				rock,
				NavigationLandmarkPalette.SupportRockAccent(random),
				emissionStrength: 0.05f);
			root.AddChild(rock);
		}
	}

	private static Vector3 LayoutPoint(
		int layout,
		int index,
		int count,
		float clusterRadius,
		RandomNumberGenerator random,
		float scatter)
	{
		var t = count <= 1 ? 0.5f : (float)index / (count - 1);
		Vector3 basePoint = layout switch
		{
			0 => new Vector3(
				(t - 0.5f) * clusterRadius * 1.35f,
				0f,
				(t - 0.5f) * clusterRadius * 0.75f),
			1 => new Vector3(
				Mathf.Cos(Mathf.Pi * 0.25f + t * Mathf.Pi * 1.15f) * clusterRadius * 0.85f,
				0f,
				Mathf.Sin(Mathf.Pi * 0.25f + t * Mathf.Pi * 1.15f) * clusterRadius * 0.85f),
			_ => ThreeMassPoint(index, count, clusterRadius),
		};

		var jitter = clusterRadius * 0.08f * scatter;
		return basePoint + new Vector3(
			(random.Randf() - 0.5f) * jitter,
			(random.Randf() - 0.5f) * jitter * 0.35f,
			(random.Randf() - 0.5f) * jitter);
	}

	private static Vector3 ThreeMassPoint(int index, int count, float clusterRadius)
	{
		Vector3[] anchors =
		[
			new(-clusterRadius * 0.28f, 0f, -clusterRadius * 0.18f),
			new(clusterRadius * 0.32f, 0f, clusterRadius * 0.12f),
			new(-clusterRadius * 0.05f, 0f, clusterRadius * 0.34f),
		];
		var anchor = anchors[index % anchors.Length];
		var ring = (float)(index % 3) / Mathf.Max(count, 1);
		return anchor + new Vector3(ring * clusterRadius * 0.12f, 0f, ring * clusterRadius * 0.08f);
	}

	private static void AddTailingsField(
		Node3D root,
		float worldRadius,
		RandomNumberGenerator random,
		int visualSeed)
	{
		AddAsteroidFormation(root, worldRadius * 0.92f, random);
		AddTailingsAccents(root, worldRadius, random);
		NavigationLandmarkDustVisuals.AddCloud(
			root,
			worldRadius,
			random,
			visualSeed,
			opacityScale: 0.55f);
	}

	private static void AddTailingsAccents(Node3D root, float worldRadius, RandomNumberGenerator random)
	{
		var count = 4 + random.Randi() % 4;
		for (var i = 0; i < count; i++)
		{
			var angle = random.Randf() * Mathf.Tau;
			var distance = random.Randf() * worldRadius * 0.55f;
			var shard = NavigationLandmarkRockLibrary.CreateRock(
				random,
				worldRadius * (0.04f + random.Randf() * 0.06f),
				mainMass: false);
			shard.Position = new Vector3(
				Mathf.Cos(angle) * distance,
				(random.Randf() - 0.5f) * worldRadius * 0.03f,
				Mathf.Sin(angle) * distance);
			shard.RotationDegrees = new Vector3(random.Randf() * 180f, random.Randf() * 360f, 0f);
			NavigationLandmarkRockLibrary.TintMeshes(
				shard,
				new Color(0.62f, 0.34f, 0.18f).Lerp(new Color(0.48f, 0.28f, 0.16f), random.Randf()),
				roughness: 0.72f);
			foreach (var node in shard.FindChildren("*", "MeshInstance3D", true, false))
			{
				if (node is MeshInstance3D mesh && mesh.MaterialOverride is StandardMaterial3D material)
				{
					material.Metallic = 0.55f;
				}
			}

			root.AddChild(shard);
		}
	}

	private static void AddDebrisFallback(Node3D root, float worldRadius, RandomNumberGenerator random)
	{
		var count = 6 + random.Randi() % 5;
		for (var i = 0; i < count; i++)
		{
			var angle = random.Randf() * Mathf.Tau;
			var distance = random.Randf() * worldRadius * 0.65f;
			var piece = NavigationLandmarkRockLibrary.CreateRock(
				random,
				worldRadius * (0.06f + random.Randf() * 0.1f),
				mainMass: i < 2);
			piece.Position = new Vector3(
				Mathf.Cos(angle) * distance,
				(random.Randf() - 0.5f) * worldRadius * 0.04f,
				Mathf.Sin(angle) * distance);
			piece.RotationDegrees = new Vector3(random.Randf() * 45f, random.Randf() * 360f, random.Randf() * 45f);
			NavigationLandmarkRockLibrary.TintMeshes(
				piece,
				NavigationLandmarkPalette.SupportRockAccent(random),
				roughness: 0.78f);
			root.AddChild(piece);
		}
	}

	private static void AddSurveyRelayFallback(Node3D root, float worldRadius, RandomNumberGenerator random)
	{
		var mastHeight = worldRadius * 0.35f;
		root.AddChild(new MeshInstance3D
		{
			Mesh = new CylinderMesh { TopRadius = 0.02f, BottomRadius = 0.03f, Height = mastHeight },
			Position = new Vector3(0f, mastHeight * 0.5f, 0f),
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = new Color(0.12f, 0.22f, 0.28f),
				Metallic = 0.6f,
				Roughness = 0.45f,
			},
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		});

		root.AddChild(new MeshInstance3D
		{
			Mesh = new SphereMesh { Radius = worldRadius * 0.16f, Height = worldRadius * 0.08f },
			Position = new Vector3(0f, mastHeight + worldRadius * 0.04f, 0f),
			RotationDegrees = new Vector3(35f, random.Randf() * 40f, 0f),
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = new Color(0.22f, 0.38f, 0.44f),
				Metallic = 0.7f,
				Roughness = 0.35f,
			},
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		});

		root.AddChild(new MeshInstance3D
		{
			Mesh = new SphereMesh { Radius = 0.035f },
			Position = new Vector3(0.06f, mastHeight * 0.72f, 0.05f),
			MaterialOverride = new StandardMaterial3D
			{
				Emission = new Color(0.18f, 0.72f, 0.62f),
				EmissionEnergyMultiplier = 0.65f,
			},
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		});
	}
}
