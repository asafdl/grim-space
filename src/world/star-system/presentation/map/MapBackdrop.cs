using Godot;
using GrimSpace.World.StarSystem.Presentation.Picking;
using GrimSpace.World.StarSystem.Presentation.Atmosphere;

namespace GrimSpace.World.StarSystem.Presentation.Map;

/// <summary>
/// Distant finite star panorama with a nebula glow. The world sky remains available
/// for lighting, while the rendered sphere gives map travel subtle parallax.
/// </summary>
public sealed partial class MapBackdrop : Node3D
{
	private const string SkyShaderPath = "res://assets/shaders/map_sky.gdshader";
	private const string PanoramaShaderPath = "res://assets/shaders/map_panorama.gdshader";
	private const string StarsTexturePath = "res://assets/textures/8k_stars.jpg";
	private const string NebulaTexturePath = "res://assets/textures/messier_17.jpg";
	private const string HorizonTexturePath = "res://assets/textures/event-horizon.jpg";
	private const float PanoramaRadius = 96f;

	public void Build(MapAtmosphereSettings? settings = null)
	{
		var atmosphere = settings ?? MapAtmosphereSettings.Default;
		AddChild(CreateWorldEnvironment(atmosphere));
		AddChild(CreateFinitePanorama(atmosphere));
	}

	private static WorldEnvironment CreateWorldEnvironment(MapAtmosphereSettings settings)
	{
		var skyMaterial = new ShaderMaterial
		{
			Shader = GD.Load<Shader>(SkyShaderPath),
		};
		ConfigurePanoramaMaterial(skyMaterial, settings);

		var sky = new Sky
		{
			SkyMaterial = skyMaterial,
			ProcessMode = Sky.ProcessModeEnum.Quality,
		};

		return new WorldEnvironment
		{
			Name = "MapEnvironment",
			Environment = new Godot.Environment
			{
				BackgroundMode = Godot.Environment.BGMode.Color,
				BackgroundColor = Colors.Black,
				Sky = sky,
				AmbientLightSource = Godot.Environment.AmbientSource.Color,
				AmbientLightColor = new Color(0.17f, 0.20f, 0.26f),
				AmbientLightEnergy = settings.AmbientEnergy,
				ReflectedLightSource = Godot.Environment.ReflectionSource.Sky,
				TonemapMode = Godot.Environment.ToneMapper.Filmic,
				FogEnabled = false,
			},
		};
	}

	private static MeshInstance3D CreateFinitePanorama(MapAtmosphereSettings settings)
	{
		var material = new ShaderMaterial
		{
			Shader = GD.Load<Shader>(PanoramaShaderPath),
		};
		ConfigurePanoramaMaterial(material, settings);

		return new MeshInstance3D
		{
			Name = "FiniteStarPanorama",
			Mesh = new SphereMesh
			{
				Radius = PanoramaRadius,
				Height = PanoramaRadius * 2f,
				RadialSegments = 128,
				Rings = 64,
			},
			MaterialOverride = material,
		};
	}

	private static void ConfigurePanoramaMaterial(
		ShaderMaterial material,
		MapAtmosphereSettings settings)
	{
		material.SetShaderParameter("stars_panorama", GD.Load<Texture2D>(StarsTexturePath));
		material.SetShaderParameter("nebula_panorama", GD.Load<Texture2D>(NebulaTexturePath));
		material.SetShaderParameter("horizon_panorama", GD.Load<Texture2D>(HorizonTexturePath));
		material.SetShaderParameter("energy", settings.StarfieldEnergy);
		material.SetShaderParameter("nebula_strength", settings.NebulaStrength);
		material.SetShaderParameter("nebula_center", settings.NebulaDirection.Normalized());
		material.SetShaderParameter("nebula_radius", settings.NebulaAngularRadius);
		material.SetShaderParameter("nebula_aspect", settings.NebulaAspect);
		material.SetShaderParameter("horizon_strength", settings.HorizonStrength);
		material.SetShaderParameter("horizon_center", settings.HorizonDirection.Normalized());
		material.SetShaderParameter("horizon_radius", settings.HorizonAngularRadius);
		material.SetShaderParameter("horizon_aspect", settings.HorizonAspect);
		material.SetShaderParameter("horizon_uv_flip", new Vector2(-1f, -1f));
	}
}
