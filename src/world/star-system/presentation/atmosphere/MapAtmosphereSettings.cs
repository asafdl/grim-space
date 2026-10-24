using Godot;
using GrimSpace.World.StarSystem.Presentation.Picking;

namespace GrimSpace.World.StarSystem.Presentation.Atmosphere;

/// <summary>
/// Tunable map atmosphere controls. Defaults target a quiet, expansive system view
/// that stays visually distinct from battle's enclosed chamber.
/// </summary>
public sealed record MapAtmosphereSettings(
	float StarfieldEnergy = 0.26f,
	float HorizonStrength = 0.36f,
	float HorizonAngularRadius = 0.12f,
	float HorizonAspect = 1.778f,
	float SunGlowEnergy = 0.78f,
	float AmbientEnergy = 0.44f,
	float FillLightEnergy = 0.17f)
{
	/// <summary>Same sky band as the star field's negative-Y band so the top-down orbit camera sees both.</summary>
	public Vector3 HorizonDirection { get; init; } = new(-0.62f, -0.38f, -0.52f);

	public static MapAtmosphereSettings Default { get; } = new();
}
