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

	public void Build(MapAtmosphereSettings? settings = null)
	{
		var atmosphere = settings ?? MapAtmosphereSettings.Default;
		AddChild(CreateWorldEnvironment(atmosphere));
	}

	private static WorldEnvironment CreateWorldEnvironment(MapAtmosphereSettings settings)
	{
		var skyMaterial = new ShaderMaterial
		{
			Shader = GD.Load<Shader>(SkyShaderPath),
		};
		skyMaterial.SetShaderParameter("stars_panorama", GD.Load<Texture2D>(StarsTexturePath));
		skyMaterial.SetShaderParameter("nebula_panorama", GD.Load<Texture2D>(NebulaTexturePath));
		skyMaterial.SetShaderParameter("energy", settings.StarfieldEnergy);
		skyMaterial.SetShaderParameter("nebula_strength", settings.NebulaStrength);
		skyMaterial.SetShaderParameter("nebula_center", settings.NebulaDirection.Normalized());
		skyMaterial.SetShaderParameter("nebula_radius", settings.NebulaAngularRadius);
		skyMaterial.SetShaderParameter("nebula_aspect", settings.NebulaAspect);

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
				BackgroundMode = Godot.Environment.BGMode.Sky,
				Sky = sky,
				AmbientLightSource = Godot.Environment.AmbientSource.Color,
				AmbientLightColor = new Color(0.16f, 0.19f, 0.24f),
				AmbientLightEnergy = settings.AmbientEnergy,
				ReflectedLightSource = Godot.Environment.ReflectionSource.Disabled,
				TonemapMode = Godot.Environment.ToneMapper.Filmic,
				FogEnabled = false,
			},
		};
	}
}
