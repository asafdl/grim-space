using Godot;
using GrimSpace.World.StarSystem.Presentation.Atmosphere;

namespace GrimSpace.World.StarSystem.Presentation;

/// <summary>
/// Distant charcoal void with a dim star panorama. Keeps the playfield readable and
/// visually separate from battle's brighter enclosed chamber sky.
/// </summary>
public sealed partial class MapBackdrop : Node3D
{
	private const string StarsTexturePath = "res://assets/textures/8k_stars.jpg";

	public void Build(MapAtmosphereSettings? settings = null)
	{
		var atmosphere = settings ?? MapAtmosphereSettings.Default;
		AddChild(CreateWorldEnvironment(atmosphere));
	}

	private static WorldEnvironment CreateWorldEnvironment(MapAtmosphereSettings settings)
	{
		var stars = GD.Load<Texture2D>(StarsTexturePath);
		var sky = new Sky
		{
			SkyMaterial = new PanoramaSkyMaterial
			{
				Panorama = stars,
				EnergyMultiplier = settings.StarfieldEnergy,
			},
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
