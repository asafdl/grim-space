using Godot;
using GrimSpace.World.StarSystem.Presentation.Camera;
using GrimSpace.World.StarSystem.Presentation.Picking;
using GrimSpace.Math;
using GrimSpace.Presentation.Graphics;
using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.World.StarSystem.Presentation.Atmosphere;

public static class MapCelestialVisuals
{
	private const string PlanetPackPath = "res://assets/models/planets/various_planets.glb";
	private static readonly (string Body, string[] Clouds)[] PlanetNodes =
	[
		("planet_smac_0", ["planet_smac_cloud_1"]),
		("planet_gas_2", ["planet_gas_cloud_01_3", "planet_gas_cloud_02_9"]),
		("planet_continental_4", ["planet_continental_clouds_5"]),
		("planet_frozen_6", []),
		("planet_lava_7", []),
		("planet_barren_8", []),
	];
	private static readonly int[] MoonletVariants = [0, 2, 3, 5];
	private static PlanetVariant[]? _planets;

	private static readonly Color[] AtmosphereTints =
	[
		new(0.62f, 0.66f, 0.74f),
		new(0.58f, 0.60f, 0.64f),
		new(0.64f, 0.70f, 0.76f),
		new(0.60f, 0.58f, 0.56f),
		new(0.86f, 0.46f, 0.26f),
		new(0.66f, 0.62f, 0.56f),
	];

	public static void AddStar(
		Node3D root,
		int seed,
		int gridRadius,
		MapAtmosphereSettings settings)
	{
		var visualRadius = gridRadius * MapMapping.WorldUnitsPerPoint;
		var palette = new SunPalette
		{
			// The strategic map star is a background element that is usually
			// only a few dozen pixels across, so the granulation is kept coarse
			// enough not to alias and the prominences are dialled down.
			GranulationCells = 34f,
			GranulationStrength = 0.55f,
			PlageCells = 6f,
			PlageStrength = 0.45f,
			ProminenceCount = 3f,
			ProminenceHeight = 0.18f,
			ProminenceStrength = 0.35f,
			CoronaStrength = 0.10f,
			EmissionStrength = 0.5f + settings.SunGlowEnergy * 0.25f,
			// The map lights its own star through MapStarLighting; the star does
			// not need to light the map back.
			LightEnergy = 0f,
			Seed = StableSeedMixer.From(seed).Add("map-star").Value % 997f,
		};
		root.AddChild(SunVisual.Create(visualRadius, palette));
		MapCameraOcclusion.AddSphere(root, visualRadius);
	}

	public static void AddPlanet(
		Node3D root,
		int variant,
		MapAtmosphereSettings settings)
	{
		if (variant < 0 || variant >= PlanetNodes.Length)
			throw new ArgumentOutOfRangeException(nameof(variant));

		EnsurePlanetsLoaded();
		var atmosphere = AtmosphereTints[variant];
		var bodyRadius = 0.55f;

		AddPlanetBody(root, _planets![variant], bodyRadius, includeClouds: true);

		root.AddChild(new MeshInstance3D
		{
			Name = "Atmosphere",
			Mesh = CreateSphere(bodyRadius * 1.08f, 36, 18),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = CreateAtmosphereMaterial(atmosphere, settings.SunGlowEnergy * 0.18f),
		});
	}

	public static IReadOnlyDictionary<string, int> AssignPlanetVariants(int seed, IEnumerable<string> poiIds)
	{
		var ids = poiIds.OrderBy(id => id, StringComparer.Ordinal).ToArray();
		if (ids.Length > PlanetNodes.Length)
			throw new InvalidOperationException(
				$"Cannot assign {ids.Length} unique planet variants from {PlanetNodes.Length} available bodies.");

		var variants = Enumerable.Range(0, PlanetNodes.Length).ToArray();
		var random = new StableRandom(StableSeedMixer.From(seed).Add("map-planet-variants").Value);
		for (var i = variants.Length - 1; i > 0; i--)
		{
			var j = (int)(random.NextDouble() * (i + 1));
			(variants[i], variants[j]) = (variants[j], variants[i]);
		}

		return ids.Select((id, index) => (id, variant: variants[index]))
			.ToDictionary(pair => pair.id, pair => pair.variant, StringComparer.Ordinal);
	}

	public static void AddMoonlet(Node3D root, int visualSeed, float worldRadius)
	{
		EnsurePlanetsLoaded();
		var variant = (int)(StableSeedMixer.From(visualSeed).Add("nav-moonlet").Value
			% (ulong)MoonletVariants.Length);
		var bodyRadius = Mathf.Clamp(worldRadius * 0.55f, 0.14f, 0.42f);

		AddPlanetBody(root, _planets![MoonletVariants[variant]], bodyRadius, includeClouds: false);
	}

	private static void AddPlanetBody(
		Node3D root,
		PlanetVariant planet,
		float radius,
		bool includeClouds)
	{
		root.AddChild(new MeshInstance3D
		{
			Name = "Body",
			Mesh = planet.Body,
			Scale = Vector3.One * radius,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		});

		if (!includeClouds)
			return;

		for (var i = 0; i < planet.Clouds.Count; i++)
		{
			var cloud = planet.Clouds[i];
			root.AddChild(new MeshInstance3D
			{
				Name = $"Clouds_{i}",
				Mesh = cloud.Mesh,
				Scale = Vector3.One * (radius * cloud.Scale),
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			});
		}
	}

	private static void EnsurePlanetsLoaded()
	{
		if (_planets is not null)
			return;

		var scene = GD.Load<PackedScene>(PlanetPackPath)
			?? throw new InvalidOperationException($"Could not load planet pack '{PlanetPackPath}'.");
		var root = scene.Instantiate<Node3D>();
		try
		{
			_planets = PlanetNodes.Select(entry =>
			{
				var body = FindPlanetMesh(root, entry.Body);
				var clouds = entry.Clouds.Select(name =>
				{
					var mesh = FindPlanetMesh(root, name);
					var parent = (Node3D)mesh.GetParent();
					return new CloudLayer(mesh.Mesh, parent.Scale.X);
				}).ToArray();
				return new PlanetVariant(body.Mesh, clouds);
			}).ToArray();
		}
		finally
		{
			root.Free();
		}
	}

	private static MeshInstance3D FindPlanetMesh(Node3D root, string nodeName)
	{
		if (root.FindChild(nodeName, recursive: true, owned: false) is not Node3D node
			|| node.FindChildren("*", "MeshInstance3D", true, false).SingleOrDefault()
				is not MeshInstance3D { Mesh: not null } mesh)
			throw new InvalidOperationException($"Planet pack '{PlanetPackPath}' is missing mesh '{nodeName}'.");

		return mesh;
	}

	private static SphereMesh CreateSphere(float radius, int radialSegments, int rings) =>
		new()
		{
			Radius = radius,
			Height = radius * 2f,
			RadialSegments = radialSegments,
			Rings = rings,
		};

	private static ShaderMaterial CreateAtmosphereMaterial(Color tint, float emissionEnergy)
	{
		var material = new ShaderMaterial
		{
			Shader = new Shader
			{
				Code =
					"""
					shader_type spatial;

					render_mode
						unshaded,
						blend_add,
						depth_draw_never,
						cull_back,
						shadows_disabled,
						fog_disabled;

					uniform vec3 atmosphere_tint : source_color;
					uniform float rim_strength : hint_range(0.0, 1.0) = 0.24;

					void fragment()
					{
						float rim = pow(
							1.0 - max(dot(normalize(NORMAL), normalize(VIEW)), 0.0),
							2.6);
						ALBEDO = atmosphere_tint;
						EMISSION = atmosphere_tint * rim * rim_strength;
						ALPHA = rim * 0.22;
					}
					""",
			},
			RenderPriority = 1,
		};
		material.SetShaderParameter("atmosphere_tint", tint);
		material.SetShaderParameter("rim_strength", emissionEnergy);
		return material;
	}

	private sealed record CloudLayer(Mesh Mesh, float Scale);
	private sealed record PlanetVariant(Mesh Body, IReadOnlyList<CloudLayer> Clouds);
}
