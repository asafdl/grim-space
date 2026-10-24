using Godot;
using GrimSpace.World.StarSystem.Presentation.Picking;
using GrimSpace.World.StarSystem.Presentation.Atmosphere;

namespace GrimSpace.World.StarSystem.Presentation.Map;

/// <summary>
/// Star panorama with a nebula glow spliced in via a composite sky shader. Keeps the
/// playfield readable and visually distinct from battle's enclosed chamber sky.
/// </summary>
public sealed partial class MapBackdrop : Node3D
{
	private const string SkyShaderPath = "res://assets/shaders/map_sky.gdshader";
	private const string StarsTexturePath = "res://assets/textures/8k_stars.jpg";
	private const string NebulaTexturePath = "res://assets/textures/messier_17.jpg";
	private const string HorizonTexturePath = "res://assets/textures/event-horizon.jpg";

	/// <summary>
	/// Builds the backdrop.  <paramref name="noSky"/> renders the sky in
	/// BGMode.Color instead: headless movie renders crash on the dummy
	/// renderer's null sky texture (<c>texture_2d_get</c>, Godot 4.7), so the
	/// audit command passes <c>--no-sky</c>; windowed renders keep the sky.
	/// </summary>
	public void Build(MapAtmosphereSettings? settings = null, bool noSky = false)
	{
		var atmosphere = settings ?? MapAtmosphereSettings.Default;
		AddChild(CreateWorldEnvironment(atmosphere, noSky));
	}

	private static WorldEnvironment CreateWorldEnvironment(MapAtmosphereSettings settings, bool noSky)
	{
		var skyMaterial = new ShaderMaterial
		{
			Shader = GD.Load<Shader>(SkyShaderPath),
		};
		skyMaterial.SetShaderParameter("stars_panorama", GD.Load<Texture2D>(StarsTexturePath));
		skyMaterial.SetShaderParameter("nebula_panorama", GD.Load<Texture2D>(NebulaTexturePath));
		skyMaterial.SetShaderParameter("horizon_panorama", GD.Load<Texture2D>(HorizonTexturePath));
		skyMaterial.SetShaderParameter("energy", settings.StarfieldEnergy);
		skyMaterial.SetShaderParameter("nebula_strength", settings.NebulaStrength);
		skyMaterial.SetShaderParameter("nebula_center", settings.NebulaDirection.Normalized());
		skyMaterial.SetShaderParameter("nebula_radius", settings.NebulaAngularRadius);
		skyMaterial.SetShaderParameter("nebula_aspect", settings.NebulaAspect);
		skyMaterial.SetShaderParameter("horizon_strength", settings.HorizonStrength);
		skyMaterial.SetShaderParameter("horizon_center", settings.HorizonDirection.Normalized());
		skyMaterial.SetShaderParameter("horizon_radius", settings.HorizonAngularRadius);
		skyMaterial.SetShaderParameter("horizon_aspect", settings.HorizonAspect);
		skyMaterial.SetShaderParameter("horizon_uv_flip", new Vector2(-1f, -1f));

		var sky = new Sky
		{
			SkyMaterial = skyMaterial,
			ProcessMode = Sky.ProcessModeEnum.Quality,
		};

		var environment = noSky
			? new Godot.Environment
			{
				// The headless-audit colour: the horizon falloff of the night
				// side, close enough that the playfield reads the same.
				BackgroundMode = Godot.Environment.BGMode.Color,
				BackgroundColor = new Color(0.03f, 0.04f, 0.06f),
				AmbientLightSource = Godot.Environment.AmbientSource.Color,
				AmbientLightColor = new Color(0.17f, 0.20f, 0.26f),
				AmbientLightEnergy = settings.AmbientEnergy,
				TonemapMode = Godot.Environment.ToneMapper.Filmic,
				FogEnabled = false,
			}
			: new Godot.Environment
			{
				BackgroundMode = Godot.Environment.BGMode.Sky,
				Sky = sky,
				AmbientLightSource = Godot.Environment.AmbientSource.Color,
				AmbientLightColor = new Color(0.17f, 0.20f, 0.26f),
				AmbientLightEnergy = settings.AmbientEnergy,
				ReflectedLightSource = Godot.Environment.ReflectionSource.Sky,
				TonemapMode = Godot.Environment.ToneMapper.Filmic,
				FogEnabled = false,
			};

		return new WorldEnvironment
		{
			Name = "MapEnvironment",
			Environment = environment,
		};
	}
}
