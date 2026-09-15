using Godot;
using GrimSpace.Math;
using GrimSpace.Presentation.Graphics;
using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.World.StarSystem.Presentation.Atmosphere;

public static class MapCelestialVisuals
{
	private const string SunTexturePath = "res://assets/textures/2k_sun.jpg";
	private const string SmokeTexturePath =
		"res://assets/kenny-particle-pack/PNG (Transparent)/smoke_02.png";

	private static readonly string[] PlanetTexturePaths =
	[
		"res://assets/textures/2k_venus_surface.jpg",
		"res://assets/textures/2k_ceres_fictional.jpg",
		"res://assets/textures/2k_haumea_fictional.jpg",
		"res://assets/textures/2k_makemake_fictional.jpg",
	];

	private static readonly Color[] PlanetTints =
	[
		new(0.58f, 0.52f, 0.48f),
		new(0.48f, 0.46f, 0.44f),
		new(0.50f, 0.54f, 0.58f),
		new(0.52f, 0.48f, 0.44f),
	];

	private static readonly Color[] AtmosphereTints =
	[
		new(0.62f, 0.66f, 0.74f),
		new(0.58f, 0.60f, 0.64f),
		new(0.64f, 0.70f, 0.76f),
		new(0.60f, 0.58f, 0.56f),
	];

	public static void AddStar(
		Node3D root,
		int seed,
		int gridRadius,
		MapAtmosphereSettings settings)
	{
		var visualRadius = gridRadius * MapMapping.WorldUnitsPerPoint;
		var surfaceTexture = GD.Load<Texture2D>(SunTexturePath);
		root.AddChild(new MeshInstance3D
		{
			Name = "Surface",
			Mesh = CreateSphere(visualRadius, 48, 24),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = CreateStarSurfaceMaterial(
				surfaceTexture,
				settings.SunGlowEnergy),
		});
		root.AddChild(StarCorona.Create(
			sunRadius: visualRadius,
			color: new Color(1f, 0.50f, 0.26f, 0.72f),
			brightness: 0.10f + settings.SunGlowEnergy * 0.12f,
			width: 0.13f,
			speed: 0f,
			seed: StableSeedMixer.From(seed).Add("map-star-corona").Value % 997f,
			irregularity: 0.22f));
	}

	public static void AddPlanet(
		Node3D root,
		int seed,
		string poiId,
		MapAtmosphereSettings settings)
	{
		var random = new StableRandom(StableSeedMixer.From(seed).Add(poiId).Add("map-planet").Value);
		var variant = (int)(random.NextDouble() * PlanetTexturePaths.Length);
		var texture = GD.Load<Texture2D>(PlanetTexturePaths[variant]);
		var tint = PlanetTints[variant];
		var atmosphere = AtmosphereTints[variant];
		var bodyRadius = 0.55f;

		root.AddChild(new MeshInstance3D
		{
			Name = "Body",
			Mesh = CreateSphere(bodyRadius, 40, 20),
			MaterialOverride = CreatePlanetSurfaceMaterial(texture, tint),
		});

		root.AddChild(new MeshInstance3D
		{
			Name = "Atmosphere",
			Mesh = CreateSphere(bodyRadius * 1.045f, 36, 18),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = CreateAtmosphereMaterial(atmosphere, settings.SunGlowEnergy * 0.18f),
		});
	}

	public static void AddMiningDust(
		Node3D root,
		int seed,
		PointOfInterest poi,
		MapAtmosphereSettings settings)
	{
		var random = new StableRandom(StableSeedMixer.From(seed).Add(poi.Id).Add("map-dust").Value);
		var worldRadius = poi.Radius * MapMapping.WorldUnitsPerPoint;
		var amount = Mathf.Clamp(
			(int)(28f + worldRadius * 18f * settings.DustDensity),
			18,
			72);
		var texture = GD.Load<Texture2D>(SmokeTexturePath);
		root.AddChild(CreateDustLayer(texture, worldRadius, amount, settings.DustOpacity, random));
	}

	private static MultiMeshInstance3D CreateDustLayer(
		Texture2D texture,
		float worldRadius,
		int amount,
		float opacity,
		StableRandom random)
	{
		var drawMaterial = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			BlendMode = BaseMaterial3D.BlendModeEnum.Mix,
			DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
			AlbedoTexture = texture,
			VertexColorUseAsAlbedo = true,
			BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
			BillboardKeepScale = true,
		};
		var multiMesh = new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			UseColors = true,
			InstanceCount = amount,
			Mesh = new QuadMesh
			{
				Size = Vector2.One * (0.55f + worldRadius * 0.22f),
				Material = drawMaterial,
			},
		};

		for (var i = 0; i < amount; i++)
		{
			var angle = random.NextDouble() * System.Math.Tau;
			var distance = random.NextDouble() * worldRadius * 0.88;
			var lift = (random.NextDouble() - 0.5) * worldRadius * 0.18;
			var position = new Vector3(
				(float)(System.Math.Cos(angle) * distance),
				(float)lift,
				(float)(System.Math.Sin(angle) * distance));
			var scale = 0.7f + (float)random.NextDouble() * 1.1f;
			multiMesh.SetInstanceTransform(
				i,
				new Transform3D(Basis.Identity.Scaled(Vector3.One * scale), position));

			var tone = 0.42f + (float)random.NextDouble() * 0.18f;
			var alpha = opacity * (0.45f + (float)random.NextDouble() * 0.55f);
			multiMesh.SetInstanceColor(
				i,
				new Color(tone * 0.72f, tone * 0.66f, tone * 0.58f, alpha));
		}

		return new MultiMeshInstance3D
		{
			Name = "RegionalDust",
			Multimesh = multiMesh,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
	}

	private static SphereMesh CreateSphere(float radius, int radialSegments, int rings) =>
		new()
		{
			Radius = radius,
			Height = radius * 2f,
			RadialSegments = radialSegments,
			Rings = rings,
		};

	private static ShaderMaterial CreateStarSurfaceMaterial(Texture2D texture, float glowEnergy)
	{
		var material = new ShaderMaterial
		{
			Shader = new Shader
			{
				Code =
					"""
					shader_type spatial;

					render_mode unshaded;

					uniform sampler2D surface_texture : source_color;
					uniform float emission_strength : hint_range(0.0, 4.0) = 1.2;
					uniform float limb_boost : hint_range(0.0, 1.0) = 0.35;

					void fragment()
					{
						vec3 source = texture(surface_texture, UV).rgb;
						float luma = dot(source, vec3(0.299, 0.587, 0.114));
						// Incandescent remap: dark granules stay orange, bright spots go yellow-white.
						vec3 granule = vec3(1.05, 0.42, 0.10);
						vec3 flare = vec3(1.25, 0.98, 0.62);
						vec3 core = mix(granule, flare, smoothstep(0.18, 0.82, luma));
						float rim = pow(
							1.0 - max(dot(normalize(NORMAL), normalize(VIEW)), 0.0),
							2.6);
						vec3 limb = vec3(1.18, 0.72, 0.22);
						vec3 color = mix(core, limb, rim * 0.5) * emission_strength;
						color *= 1.0 + rim * limb_boost;
						ALBEDO = color * 0.72;
						EMISSION = color;
					}
					""",
			},
		};
		material.SetShaderParameter("surface_texture", texture);
		material.SetShaderParameter("emission_strength", 0.82f + glowEnergy * 0.62f);
		material.SetShaderParameter("limb_boost", 0.08f + glowEnergy * 0.10f);
		return material;
	}

	private static ShaderMaterial CreatePlanetSurfaceMaterial(Texture2D texture, Color tint)
	{
		var material = new ShaderMaterial
		{
			Shader = new Shader
			{
				Code =
					"""
					shader_type spatial;

					uniform sampler2D surface_texture : source_color;
					uniform vec3 surface_tint : source_color;

					void fragment()
					{
						vec3 source = texture(surface_texture, UV).rgb;
						float luma = dot(source, vec3(0.299, 0.587, 0.114));
						vec3 shadow = surface_tint * 0.34;
						vec3 lit = surface_tint * 0.82;
						ALBEDO = mix(shadow, lit, smoothstep(0.08, 0.92, luma));
						ROUGHNESS = 0.94;
					}
					""",
			},
		};
		material.SetShaderParameter("surface_texture", texture);
		material.SetShaderParameter("surface_tint", tint);
		return material;
	}

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

}
