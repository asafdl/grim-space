using Godot;
using GrimSpace.World.StarSystem.Presentation.Picking;
using GrimSpace.World.StarSystem.Poi.Concrete;

namespace GrimSpace.World.StarSystem.Presentation.Atmosphere;

public static class MapStarLighting
{
	private static readonly Color KeyLightColor = new(1.00f, 0.86f, 0.68f);
	private static readonly Color FillLightColor = new(0.58f, 0.66f, 0.82f);

	public static void Configure(
		DirectionalLight3D keyLight,
		DirectionalLight3D fillLight,
		Star star,
		int width,
		int height,
		MapAtmosphereSettings settings)
	{
		var starWorld = MapMapping.ToWorld(star.PlacedCenter, width, height);
		var toMap = Vector3.Zero - starWorld;
		var horizontal = new Vector3(toMap.X, 0f, toMap.Z);
		if (horizontal.LengthSquared() < 0.0001f)
			horizontal = Vector3.Forward;
		else
			horizontal = horizontal.Normalized();

		// Star motivates azimuth; upward tilt keeps map-plane tops readable (not grazing light).
		var keyDirection = (horizontal + Vector3.Up * 0.42f).Normalized();
		keyLight.GlobalPosition = starWorld;
		keyLight.LookAt(starWorld + keyDirection, Vector3.Up);
		keyLight.LightColor = KeyLightColor;
		keyLight.LightEnergy = 0.24f + settings.SunGlowEnergy * 0.42f;
		keyLight.ShadowEnabled = false;

		// Cool fill from the anti-star side; stay mostly lateral so it lifts shadow faces without flattening the key.
		var fillDirection = (-horizontal + Vector3.Up * 0.34f).Normalized();
		fillLight.GlobalPosition = Vector3.Zero;
		fillLight.LookAt(fillDirection, Vector3.Up);
		fillLight.LightColor = FillLightColor;
		fillLight.LightEnergy = settings.FillLightEnergy;
		fillLight.ShadowEnabled = false;
	}
}
