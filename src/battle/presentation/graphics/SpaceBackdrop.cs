using Godot;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class SpaceBackdrop : Node3D
{
	private const string StarsTexturePath = "res://assets/textures/8k_stars.jpg";
	private const string SkyShaderPath = "res://assets/shaders/map_sky.gdshader";
	private const string NebulaTexturePath = "res://assets/textures/space/battle_nebula_panorama.png";
	private const string DustTexturePath = "res://assets/textures/particles/circle_05.png";
	private const string SmokeTexturePath = "res://assets/textures/particles/smoke_02.png";
	private const string PlanetTexturePath = "res://assets/textures/2k_venus_surface.jpg";
	private const ulong DustSeedSalt = 0xC6A4A7935BD1E995;
	private const float PlanetDistanceScale = 2.65f;
	private const float PlanetRadiusScale = 0.22f;

	public void Build(BoundedGrid grid, int seed)
	{
		var center = WorldMapping.GridCenter(grid);
		var halfExtent = new Vector3(
			grid.Width * WorldMapping.CellSize,
			grid.Height * WorldMapping.CellSize,
			grid.Depth * WorldMapping.CellSize) * 0.5f;
		var chamberRadius = halfExtent.Length();

		AddChild(CreateWorldEnvironment());
		AddChild(RedDwarfSun.CreateVisual(center, chamberRadius));
		AddChild(CreatePlanet(center, chamberRadius));
		AddChild(CreatePlanetLight(center));
		AddChild(CreateDustStream(center, halfExtent, seed));
	}

	private static WorldEnvironment CreateWorldEnvironment()
	{
		var stars = GD.Load<Texture2D>(StarsTexturePath);
		var skyMaterial = new ShaderMaterial
		{
			Shader = GD.Load<Shader>(SkyShaderPath),
		};
		skyMaterial.SetShaderParameter("stars_panorama", stars);
		skyMaterial.SetShaderParameter("nebula_panorama", GD.Load<Texture2D>(NebulaTexturePath));
		skyMaterial.SetShaderParameter(
			"horizon_panorama",
			GD.Load<Texture2D>("res://assets/textures/event-horizon.jpg"));
		skyMaterial.SetShaderParameter("energy", 0.65f);
		skyMaterial.SetShaderParameter("nebula_strength", 0.38f);
		skyMaterial.SetShaderParameter("nebula_layer_2_strength", 0.85f);
		skyMaterial.SetShaderParameter("nebula_layer_3_strength", 0.75f);
		skyMaterial.SetShaderParameter("nebula_layer_4_strength", 0.85f);
		skyMaterial.SetShaderParameter("nebula_layer_5_strength", 0.75f);
		skyMaterial.SetShaderParameter("nebula_layer_6_strength", 0.8f);
		skyMaterial.SetShaderParameter("nebula_layer_7_strength", 0.55f);
		skyMaterial.SetShaderParameter("nebula_layer_8_strength", 0.5f);
		skyMaterial.SetShaderParameter("nebula_layer_9_strength", 0.6f);
		skyMaterial.SetShaderParameter("nebula_layer_10_strength", 0.5f);
		skyMaterial.SetShaderParameter("nebula_layer_11_strength", 0.55f);
		skyMaterial.SetShaderParameter("nebula_layer_12_strength", 0.5f);
		skyMaterial.SetShaderParameter("nebula_layer_13_strength", 0.42f);
		skyMaterial.SetShaderParameter("nebula_layer_14_strength", 0.38f);
		skyMaterial.SetShaderParameter("nebula_layer_15_strength", 0.45f);
		skyMaterial.SetShaderParameter("nebula_layer_16_strength", 0.4f);
		skyMaterial.SetShaderParameter("nebula_layer_17_strength", 0.42f);
		skyMaterial.SetShaderParameter("nebula_layer_18_strength", 0.38f);
		skyMaterial.SetShaderParameter("nebula_layer_19_strength", 0.42f);
		skyMaterial.SetShaderParameter("nebula_layer_20_strength", 0.38f);
		skyMaterial.SetShaderParameter("nebula_layer_21_strength", 0.45f);
		skyMaterial.SetShaderParameter("nebula_layer_22_strength", 0.4f);
		skyMaterial.SetShaderParameter("nebula_layer_23_strength", 0.42f);
		skyMaterial.SetShaderParameter("nebula_layer_24_strength", 0.38f);
		skyMaterial.SetShaderParameter("nebula_layer_25_strength", 0.36f);
		skyMaterial.SetShaderParameter("nebula_layer_26_strength", 0.34f);
		skyMaterial.SetShaderParameter("nebula_layer_27_strength", 0.38f);
		skyMaterial.SetShaderParameter("nebula_layer_28_strength", 0.34f);
		skyMaterial.SetShaderParameter("nebula_layer_29_strength", 0.36f);
		skyMaterial.SetShaderParameter("nebula_layer_30_strength", 0.34f);
		skyMaterial.SetShaderParameter("nebula_gas_start", 0.025f);
		skyMaterial.SetShaderParameter("nebula_gas_end", 0.16f);
		skyMaterial.SetShaderParameter("nebula_mip_level", 3.5f);
		skyMaterial.SetShaderParameter("nebula_organic_edges", true);
		skyMaterial.SetShaderParameter("nebula_tint_color", new Color(1f, 0.48f, 0.2f));
		skyMaterial.SetShaderParameter("nebula_tint_color_2", new Color(1f, 0.68f, 0.3f));
		skyMaterial.SetShaderParameter("nebula_tint_color_3", new Color(0.88f, 0.34f, 0.22f));
		skyMaterial.SetShaderParameter("nebula_tint_color_4", new Color(0.28f, 0.62f, 1f));
		skyMaterial.SetShaderParameter("nebula_tint_color_5", new Color(0.68f, 0.34f, 1f));
		skyMaterial.SetShaderParameter("nebula_tint_color_6", new Color(0.2f, 0.82f, 0.72f));
		skyMaterial.SetShaderParameter("nebula_tint_color_7", new Color(0.3f, 0.68f, 1f));
		skyMaterial.SetShaderParameter("nebula_tint_color_8", new Color(1f, 0.42f, 0.38f));
		skyMaterial.SetShaderParameter("nebula_tint_color_9", new Color(0.55f, 0.4f, 1f));
		skyMaterial.SetShaderParameter("nebula_tint_color_10", new Color(0.25f, 0.78f, 0.82f));
		skyMaterial.SetShaderParameter("nebula_tint_color_11", new Color(1f, 0.64f, 0.3f));
		skyMaterial.SetShaderParameter("nebula_tint_color_12", new Color(0.78f, 0.38f, 0.72f));
		skyMaterial.SetShaderParameter("nebula_tint_color_13", new Color(0.3f, 0.68f, 1f));
		skyMaterial.SetShaderParameter("nebula_tint_color_14", new Color(1f, 0.42f, 0.38f));
		skyMaterial.SetShaderParameter("nebula_tint_color_15", new Color(0.55f, 0.4f, 1f));
		skyMaterial.SetShaderParameter("nebula_tint_color_16", new Color(0.25f, 0.78f, 0.82f));
		skyMaterial.SetShaderParameter("nebula_tint_color_17", new Color(1f, 0.64f, 0.3f));
		skyMaterial.SetShaderParameter("nebula_tint_color_18", new Color(0.78f, 0.38f, 0.72f));
		skyMaterial.SetShaderParameter("nebula_tint_color_19", new Color(0.3f, 0.68f, 1f));
		skyMaterial.SetShaderParameter("nebula_tint_color_20", new Color(1f, 0.42f, 0.38f));
		skyMaterial.SetShaderParameter("nebula_tint_color_21", new Color(0.55f, 0.4f, 1f));
		skyMaterial.SetShaderParameter("nebula_tint_color_22", new Color(0.25f, 0.78f, 0.82f));
		skyMaterial.SetShaderParameter("nebula_tint_color_23", new Color(1f, 0.64f, 0.3f));
		skyMaterial.SetShaderParameter("nebula_tint_color_24", new Color(0.78f, 0.38f, 0.72f));
		skyMaterial.SetShaderParameter("nebula_tint_color_25", new Color(0.3f, 0.68f, 1f));
		skyMaterial.SetShaderParameter("nebula_tint_color_26", new Color(1f, 0.42f, 0.38f));
		skyMaterial.SetShaderParameter("nebula_tint_color_27", new Color(0.55f, 0.4f, 1f));
		skyMaterial.SetShaderParameter("nebula_tint_color_28", new Color(0.25f, 0.78f, 0.82f));
		skyMaterial.SetShaderParameter("nebula_tint_color_29", new Color(1f, 0.64f, 0.3f));
		skyMaterial.SetShaderParameter("nebula_tint_color_30", new Color(0.78f, 0.38f, 0.72f));
		skyMaterial.SetShaderParameter("nebula_tint_strength", 0.95f);
		skyMaterial.SetShaderParameter("nebula_center", SkyDirection(0f, 34f));
		skyMaterial.SetShaderParameter("nebula_center_2", SkyDirection(120f, -34f));
		skyMaterial.SetShaderParameter("nebula_center_3", SkyDirection(240f, 34f));
		skyMaterial.SetShaderParameter("nebula_center_4", SkyDirection(0f, -34f));
		skyMaterial.SetShaderParameter("nebula_center_5", SkyDirection(120f, 34f));
		skyMaterial.SetShaderParameter("nebula_center_6", SkyDirection(240f, -34f));
		skyMaterial.SetShaderParameter("nebula_center_7", SkyDirection(60f, -34f));
		skyMaterial.SetShaderParameter("nebula_center_8", SkyDirection(180f, 34f));
		skyMaterial.SetShaderParameter("nebula_center_9", SkyDirection(300f, -34f));
		skyMaterial.SetShaderParameter("nebula_center_10", SkyDirection(60f, 34f));
		skyMaterial.SetShaderParameter("nebula_center_11", SkyDirection(180f, -34f));
		skyMaterial.SetShaderParameter("nebula_center_12", SkyDirection(300f, 34f));
		skyMaterial.SetShaderParameter("nebula_center_13", SkyDirection(30f, 0f));
		skyMaterial.SetShaderParameter("nebula_center_14", SkyDirection(90f, 58f));
		skyMaterial.SetShaderParameter("nebula_center_15", SkyDirection(150f, 0f));
		skyMaterial.SetShaderParameter("nebula_center_16", SkyDirection(210f, -58f));
		skyMaterial.SetShaderParameter("nebula_center_17", SkyDirection(270f, 0f));
		skyMaterial.SetShaderParameter("nebula_center_18", SkyDirection(330f, 58f));
		skyMaterial.SetShaderParameter("nebula_center_19", SkyDirection(30f, -58f));
		skyMaterial.SetShaderParameter("nebula_center_20", SkyDirection(90f, 0f));
		skyMaterial.SetShaderParameter("nebula_center_21", SkyDirection(150f, 58f));
		skyMaterial.SetShaderParameter("nebula_center_22", SkyDirection(210f, 0f));
		skyMaterial.SetShaderParameter("nebula_center_23", SkyDirection(270f, -58f));
		skyMaterial.SetShaderParameter("nebula_center_24", SkyDirection(330f, 0f));
		skyMaterial.SetShaderParameter("nebula_center_25", SkyDirection(0f, 22f));
		skyMaterial.SetShaderParameter("nebula_center_26", SkyDirection(60f, -22f));
		skyMaterial.SetShaderParameter("nebula_center_27", SkyDirection(120f, 22f));
		skyMaterial.SetShaderParameter("nebula_center_28", SkyDirection(180f, -22f));
		skyMaterial.SetShaderParameter("nebula_center_29", SkyDirection(240f, 22f));
		skyMaterial.SetShaderParameter("nebula_center_30", SkyDirection(300f, -22f));
		skyMaterial.SetShaderParameter("nebula_radius", 0.44f);
		skyMaterial.SetShaderParameter("nebula_radius_2", 0.4f);
		skyMaterial.SetShaderParameter("nebula_radius_3", 0.48f);
		skyMaterial.SetShaderParameter("nebula_radius_4", 0.38f);
		skyMaterial.SetShaderParameter("nebula_radius_5", 0.4f);
		skyMaterial.SetShaderParameter("nebula_radius_6", 0.35f);
		skyMaterial.SetShaderParameter("nebula_radius_7", 0.32f);
		skyMaterial.SetShaderParameter("nebula_radius_8", 0.36f);
		skyMaterial.SetShaderParameter("nebula_radius_9", 0.3f);
		skyMaterial.SetShaderParameter("nebula_radius_10", 0.34f);
		skyMaterial.SetShaderParameter("nebula_radius_11", 0.31f);
		skyMaterial.SetShaderParameter("nebula_radius_12", 0.35f);
		skyMaterial.SetShaderParameter("nebula_radius_13", 0.24f);
		skyMaterial.SetShaderParameter("nebula_radius_14", 0.23f);
		skyMaterial.SetShaderParameter("nebula_radius_15", 0.25f);
		skyMaterial.SetShaderParameter("nebula_radius_16", 0.22f);
		skyMaterial.SetShaderParameter("nebula_radius_17", 0.24f);
		skyMaterial.SetShaderParameter("nebula_radius_18", 0.23f);
		skyMaterial.SetShaderParameter("nebula_radius_19", 0.22f);
		skyMaterial.SetShaderParameter("nebula_radius_20", 0.25f);
		skyMaterial.SetShaderParameter("nebula_radius_21", 0.23f);
		skyMaterial.SetShaderParameter("nebula_radius_22", 0.24f);
		skyMaterial.SetShaderParameter("nebula_radius_23", 0.22f);
		skyMaterial.SetShaderParameter("nebula_radius_24", 0.24f);
		skyMaterial.SetShaderParameter("nebula_radius_25", 0.22f);
		skyMaterial.SetShaderParameter("nebula_radius_26", 0.23f);
		skyMaterial.SetShaderParameter("nebula_radius_27", 0.21f);
		skyMaterial.SetShaderParameter("nebula_radius_28", 0.22f);
		skyMaterial.SetShaderParameter("nebula_radius_29", 0.23f);
		skyMaterial.SetShaderParameter("nebula_radius_30", 0.21f);
		skyMaterial.SetShaderParameter("nebula_aspect", 1.7f);
		skyMaterial.SetShaderParameter("nebula_aspect_2", 1.65f);
		skyMaterial.SetShaderParameter("nebula_aspect_3", 2.3f);
		skyMaterial.SetShaderParameter("nebula_aspect_4", 1.55f);
		skyMaterial.SetShaderParameter("nebula_aspect_5", 1.8f);
		skyMaterial.SetShaderParameter("nebula_aspect_6", 2f);
		skyMaterial.SetShaderParameter("nebula_aspect_7", 1.45f);
		skyMaterial.SetShaderParameter("nebula_aspect_8", 1.9f);
		skyMaterial.SetShaderParameter("nebula_aspect_9", 1.7f);
		skyMaterial.SetShaderParameter("nebula_aspect_10", 2.1f);
		skyMaterial.SetShaderParameter("nebula_aspect_11", 1.55f);
		skyMaterial.SetShaderParameter("nebula_aspect_12", 1.85f);
		skyMaterial.SetShaderParameter("nebula_aspect_13", 1.5f);
		skyMaterial.SetShaderParameter("nebula_aspect_14", 1.9f);
		skyMaterial.SetShaderParameter("nebula_aspect_15", 1.65f);
		skyMaterial.SetShaderParameter("nebula_aspect_16", 2.05f);
		skyMaterial.SetShaderParameter("nebula_aspect_17", 1.55f);
		skyMaterial.SetShaderParameter("nebula_aspect_18", 1.8f);
		skyMaterial.SetShaderParameter("nebula_aspect_19", 2.0f);
		skyMaterial.SetShaderParameter("nebula_aspect_20", 1.6f);
		skyMaterial.SetShaderParameter("nebula_aspect_21", 1.9f);
		skyMaterial.SetShaderParameter("nebula_aspect_22", 1.7f);
		skyMaterial.SetShaderParameter("nebula_aspect_23", 2.1f);
		skyMaterial.SetShaderParameter("nebula_aspect_24", 1.6f);
		skyMaterial.SetShaderParameter("nebula_aspect_25", 1.75f);
		skyMaterial.SetShaderParameter("nebula_aspect_26", 1.55f);
		skyMaterial.SetShaderParameter("nebula_aspect_27", 1.9f);
		skyMaterial.SetShaderParameter("nebula_aspect_28", 1.65f);
		skyMaterial.SetShaderParameter("nebula_aspect_29", 1.85f);
		skyMaterial.SetShaderParameter("nebula_aspect_30", 1.6f);
		skyMaterial.SetShaderParameter("nebula_region", new Vector4(0f, 0.05f, 0.55f, 0.65f));
		skyMaterial.SetShaderParameter("nebula_region_2", new Vector4(0.42f, 0.08f, 0.56f, 0.68f));
		skyMaterial.SetShaderParameter("nebula_region_3", new Vector4(0.18f, 0.32f, 0.65f, 0.56f));
		skyMaterial.SetShaderParameter("nebula_region_4", new Vector4(0.06f, 0.34f, 0.48f, 0.58f));
		skyMaterial.SetShaderParameter("nebula_region_5", new Vector4(0.5f, 0.3f, 0.46f, 0.62f));
		skyMaterial.SetShaderParameter("nebula_region_6", new Vector4(0.25f, 0.18f, 0.58f, 0.58f));
		skyMaterial.SetShaderParameter("nebula_region_7", new Vector4(0.34f, 0.02f, 0.5f, 0.68f));
		skyMaterial.SetShaderParameter("nebula_region_8", new Vector4(0.03f, 0.23f, 0.52f, 0.56f));
		skyMaterial.SetShaderParameter("nebula_region_9", new Vector4(0.48f, 0.08f, 0.5f, 0.64f));
		skyMaterial.SetShaderParameter("nebula_region_10", new Vector4(0.17f, 0.32f, 0.54f, 0.58f));
		skyMaterial.SetShaderParameter("nebula_region_11", new Vector4(0.44f, 0.28f, 0.5f, 0.62f));
		skyMaterial.SetShaderParameter("nebula_region_12", new Vector4(0.05f, 0.13f, 0.52f, 0.64f));
		skyMaterial.SetShaderParameter("nebula_region_13", new Vector4(0.08f, 0.12f, 0.38f, 0.48f));
		skyMaterial.SetShaderParameter("nebula_region_14", new Vector4(0.53f, 0.02f, 0.38f, 0.48f));
		skyMaterial.SetShaderParameter("nebula_region_15", new Vector4(0.29f, 0.06f, 0.4f, 0.46f));
		skyMaterial.SetShaderParameter("nebula_region_16", new Vector4(0.02f, 0.5f, 0.4f, 0.44f));
		skyMaterial.SetShaderParameter("nebula_region_17", new Vector4(0.59f, 0.42f, 0.36f, 0.46f));
		skyMaterial.SetShaderParameter("nebula_region_18", new Vector4(0.2f, 0.45f, 0.4f, 0.46f));
		skyMaterial.SetShaderParameter("nebula_region_19", new Vector4(0.46f, 0.18f, 0.4f, 0.46f));
		skyMaterial.SetShaderParameter("nebula_region_20", new Vector4(0.1f, 0.32f, 0.4f, 0.46f));
		skyMaterial.SetShaderParameter("nebula_region_21", new Vector4(0.54f, 0.15f, 0.4f, 0.46f));
		skyMaterial.SetShaderParameter("nebula_region_22", new Vector4(0.25f, 0.24f, 0.42f, 0.46f));
		skyMaterial.SetShaderParameter("nebula_region_23", new Vector4(0.04f, 0.04f, 0.4f, 0.48f));
		skyMaterial.SetShaderParameter("nebula_region_24", new Vector4(0.48f, 0.4f, 0.4f, 0.46f));
		skyMaterial.SetShaderParameter("nebula_region_25", new Vector4(0.32f, 0.05f, 0.42f, 0.46f));
		skyMaterial.SetShaderParameter("nebula_region_26", new Vector4(0.04f, 0.24f, 0.42f, 0.46f));
		skyMaterial.SetShaderParameter("nebula_region_27", new Vector4(0.51f, 0.38f, 0.4f, 0.46f));
		skyMaterial.SetShaderParameter("nebula_region_28", new Vector4(0.22f, 0.1f, 0.42f, 0.46f));
		skyMaterial.SetShaderParameter("nebula_region_29", new Vector4(0.07f, 0.42f, 0.42f, 0.46f));
		skyMaterial.SetShaderParameter("nebula_region_30", new Vector4(0.43f, 0.06f, 0.42f, 0.46f));
		skyMaterial.SetShaderParameter("horizon_strength", 0f);
		var sky = new Sky
		{
			SkyMaterial = skyMaterial,
			ProcessMode = Sky.ProcessModeEnum.Quality,
		};

		return new WorldEnvironment
		{
			Name = "SpaceEnvironment",
			Environment = new Godot.Environment
			{
				BackgroundMode = Godot.Environment.BGMode.Sky,
				Sky = sky,
				AmbientLightSource = Godot.Environment.AmbientSource.Color,
				AmbientLightColor = new Color(0.12f, 0.14f, 0.18f),
				AmbientLightEnergy = 0.24f,
				ReflectedLightSource = Godot.Environment.ReflectionSource.Disabled,
				TonemapMode = Godot.Environment.ToneMapper.Filmic,
				FogEnabled = false,
			},
		};
	}

	private static Vector3 SkyDirection(float azimuthDegrees, float elevationDegrees)
	{
		var azimuth = Mathf.DegToRad(azimuthDegrees);
		var elevation = Mathf.DegToRad(elevationDegrees);
		var horizontal = Mathf.Cos(elevation);
		return new Vector3(
			horizontal * Mathf.Sin(azimuth),
			Mathf.Sin(elevation),
			horizontal * Mathf.Cos(azimuth));
	}

	private static DirectionalLight3D CreatePlanetLight(Vector3 gridCenter) =>
		new()
		{
			Name = "PurplePlanetLight",
			Position = gridCenter,
			Basis = Basis.LookingAt(-RedDwarfSun.LightDirection, Vector3.Up),
			LightColor = new Color(0.58f, 0.38f, 0.86f),
			LightEnergy = 0.28f,
			LightCullMask = PresentationLayers.World,
			ShadowEnabled = false,
		};

	private static Node3D CreatePlanet(Vector3 gridCenter, float chamberRadius)
	{
		var radius = chamberRadius * PlanetRadiusScale;
		var root = new Node3D
		{
			Name = "PurplePlanet",
			Position = gridCenter + RedDwarfSun.LightDirection * chamberRadius * PlanetDistanceScale,
			RotationDegrees = new Vector3(-12f, 28f, 8f),
		};
		var texture = GD.Load<Texture2D>(PlanetTexturePath);
		root.AddChild(new MeshInstance3D
		{
			Name = "Surface",
			Mesh = CreateSphere(radius, radialSegments: 48, rings: 24),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = CreatePlanetSurfaceMaterial(texture),
		});
		root.AddChild(new MeshInstance3D
		{
			Name = "Atmosphere",
			Mesh = CreateSphere(radius * 1.06f, radialSegments: 48, rings: 24),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = CreatePlanetAtmosphereMaterial(),
		});
		return root;
	}

	private static SphereMesh CreateSphere(float radius, int radialSegments, int rings) =>
		new()
		{
			Radius = radius,
			Height = radius * 2f,
			RadialSegments = radialSegments,
			Rings = rings,
		};

	private static ShaderMaterial CreatePlanetSurfaceMaterial(Texture2D texture)
	{
		var material = new ShaderMaterial
		{
			Shader = new Shader
			{
				Code =
					"""
					shader_type spatial;

					uniform sampler2D surface_texture : source_color;

					void fragment()
					{
						vec3 source = texture(surface_texture, UV).rgb;
						float detail = dot(source, vec3(0.299, 0.587, 0.114));
						float tone = smoothstep(0.08, 0.95, detail);
						float posterized = floor(tone * 6.0 + 0.5) / 6.0;
						tone = mix(tone, posterized, 0.42);
						ALBEDO = mix(
							vec3(0.055, 0.018, 0.09),
							vec3(0.58, 0.23, 0.72),
							tone);
						ROUGHNESS = 0.94;
					}
					""",
			},
		};
		material.SetShaderParameter("surface_texture", texture);
		return material;
	}

	private static ShaderMaterial CreatePlanetAtmosphereMaterial() =>
		new()
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

					void fragment()
					{
						float rim = pow(
							1.0 - max(dot(normalize(NORMAL), normalize(VIEW)), 0.0),
							2.2);
						vec3 haze = vec3(0.48, 0.16, 0.78);
						ALBEDO = haze;
						EMISSION = haze * rim * 0.45;
						ALPHA = rim * 0.34;
					}
					""",
			},
		};

	private static Node3D CreateDustStream(Vector3 gridCenter, Vector3 halfExtent, int seed)
	{
		var rng = new RandomNumberGenerator
		{
			Seed = (ulong)(uint)seed ^ DustSeedSalt,
		};
		var center = gridCenter + new Vector3(
			rng.RandfRange(-halfExtent.X, halfExtent.X),
			rng.RandfRange(-halfExtent.Y, halfExtent.Y),
			rng.RandfRange(-halfExtent.Z, halfExtent.Z)) * 0.18f;
		var direction = RandomUnit(rng);
		var up = Mathf.Abs(direction.Dot(Vector3.Up)) > 0.95f
			? Vector3.Right
			: Vector3.Up;
		var length = halfExtent.Length() * 2.5f;
		var width = Mathf.Min(halfExtent.X, Mathf.Min(halfExtent.Y, halfExtent.Z)) * 0.26f;
		var bend = width * rng.RandfRange(0.9f, 1.35f);
		if (rng.Randf() < 0.5f)
			bend = -bend;

		var root = new Node3D
		{
			Name = "DustStream",
			Position = center,
			Basis = Basis.LookingAt(direction, up),
		};
		root.AddChild(CreateDustLayer(
			"DiffuseDust",
			SmokeTexturePath,
			length,
			width,
			bend,
			thickness: width * 0.07f,
			amount: 1900,
			clusterCount: 22,
			particleSize: 2.2f,
			scale: new Vector2(0.65f, 1.6f),
			colors:
			[
				new Color(0.34f, 0.12f, 0.27f, 0.1f),
				new Color(0.2f, 0.13f, 0.34f, 0.09f),
				new Color(0.14f, 0.2f, 0.38f, 0.075f),
				new Color(0.42f, 0.18f, 0.16f, 0.085f),
			],
			rng: rng));
		root.AddChild(CreateDustLayer(
			"FineDust",
			DustTexturePath,
			length,
			width * 0.82f,
			bend,
			thickness: width * 0.045f,
			amount: 14000,
			clusterCount: 36,
			particleSize: 0.18f,
			scale: new Vector2(0.45f, 1.8f),
			colors:
			[
				new Color(0.95f, 0.42f, 0.68f, 0.48f),
				new Color(0.66f, 0.4f, 0.9f, 0.44f),
				new Color(0.42f, 0.58f, 0.95f, 0.38f),
				new Color(0.95f, 0.58f, 0.42f, 0.4f),
			],
			rng: rng));
		return root;
	}

	private static MultiMeshInstance3D CreateDustLayer(
		string name,
		string texturePath,
		float length,
		float width,
		float bend,
		float thickness,
		int amount,
		int clusterCount,
		float particleSize,
		Vector2 scale,
		IReadOnlyList<Color> colors,
		RandomNumberGenerator rng)
	{
		var texture = GD.Load<Texture2D>(texturePath);
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
				Size = Vector2.One * particleSize,
				Material = drawMaterial,
			},
		};
		var clusters = CreateDustClusters(length, width, bend, clusterCount, rng);
		var clusterColors = clusters
			.Select((_, index) => colors[index % colors.Count])
			.ToArray();
		for (var i = 0; i < amount; i++)
		{
			var clusterIndex = rng.RandiRange(0, clusters.Length - 1);
			var cluster = clusters[clusterIndex];
			var position = new Vector3(
				ClusterOffset(rng, cluster.X, width * 0.2f),
				ClusterOffset(rng, 0f, thickness),
				ClusterOffset(rng, cluster.Y, length / clusterCount * 1.2f));
			var instanceScale = rng.RandfRange(scale.X, scale.Y);
			var rotation = new Basis(Vector3.Forward, rng.RandfRange(0f, Mathf.Tau));
			multiMesh.SetInstanceTransform(
				i,
				new Transform3D(rotation.Scaled(Vector3.One * instanceScale), position));
			var color = clusterColors[clusterIndex];
			multiMesh.SetInstanceColor(
				i,
				color with { A = color.A * rng.RandfRange(0.45f, 1f) });
		}

		var layer = new MultiMeshInstance3D
		{
			Name = name,
			Multimesh = multiMesh,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		PresentationLayers.MarkWorld(layer);
		return layer;
	}

	private static Vector2[] CreateDustClusters(
		float length,
		float width,
		float bend,
		int count,
		RandomNumberGenerator rng)
	{
		var clusters = new Vector2[count];
		for (var i = 0; i < count; i++)
		{
			var t = (i + 0.5f) / count - 0.5f;
			clusters[i] = new Vector2(
				bend * Mathf.Sin(t * Mathf.Tau)
					+ rng.RandfRange(-width * 0.12f, width * 0.12f),
				Mathf.Lerp(-length * 0.5f, length * 0.5f, t + 0.5f)
					+ rng.RandfRange(-length / count, length / count));
		}
		return clusters;
	}

	private static float ClusterOffset(
		RandomNumberGenerator rng,
		float center,
		float spread) =>
		center + (rng.Randf() + rng.Randf() + rng.Randf() - 1.5f) * spread;

	private static Vector3 RandomUnit(RandomNumberGenerator rng)
	{
		var y = rng.RandfRange(-1f, 1f);
		var azimuth = rng.RandfRange(0f, Mathf.Tau);
		var radius = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
		return new Vector3(
			radius * Mathf.Cos(azimuth),
			y,
			radius * Mathf.Sin(azimuth));
	}
}
