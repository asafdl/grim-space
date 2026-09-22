using Godot;
using GrimSpace.World.StarSystem.Presentation.Picking;
using GrimSpace.World.StarSystem.Poi.Concrete;

namespace GrimSpace.World.StarSystem.Presentation.Atmosphere;

public static class MapStarLighting
{
	private static readonly Color LightColor = new(0.94f, 0.90f, 0.84f);

	public static void Configure(
		DirectionalLight3D light,
		Star star,
		int width,
		int height,
		MapAtmosphereSettings settings)
	{
		var starWorld = MapMapping.ToWorld(star.PlacedCenter, width, height);
		var direction = (Vector3.Zero - starWorld).Normalized();
		light.GlobalPosition = starWorld;
		light.LookAt(starWorld + direction, Vector3.Up);
		light.LightColor = LightColor;
		light.LightEnergy = 0.18f + settings.SunGlowEnergy * 0.34f;
		light.ShadowEnabled = false;
	}
}
