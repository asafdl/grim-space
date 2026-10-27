using Godot;
using GrimSpace.World.StarSystem.Presentation.Picking;

namespace GrimSpace.World.StarSystem.Presentation.Atmosphere;

/// <summary>
/// Tunable map atmosphere controls. Defaults target a quiet, expansive system view
/// that stays visually distinct from battle's enclosed chamber.
/// </summary>
public sealed record MapAtmosphereSettings(
	float StarfieldEnergy = 0.26f,
	float NebulaStrength = 0.45f,
	float NebulaAngularRadius = 0.30f,
	float NebulaAspect = 1.071f,
	// The black hole lives in the map SKY SHADER as a direction-locked
	// sky-pass feature, so its numbers are angular (radians), not world units:
	// old world-space tuning was horizon 26 / disk 50 at distance 260 => atan
	// 0.0996 / 0.190 rad.
	float BlackHoleShadowAngularRadius = 0.10f,
	float BlackHoleGlowAngularRadius = 0.19f,
	float SunGlowEnergy = 0.78f,
	float AmbientEnergy = 0.44f,
	float FillLightEnergy = 0.17f)
{
	public Vector3 NebulaDirection { get; init; } = new(0.764f, -0.404f, -0.503f);

	/// <summary>
	/// The black hole's sky direction (the sky-pass feature point); opposite-ish
	/// of <see cref="NebulaDirection"/> so one wide orbit shows both far layers
	/// separately.
	/// </summary>
	public Vector3 BlackHoleDirection { get; init; } = new(-0.62f, -0.38f, -0.52f);

	public static MapAtmosphereSettings Default { get; } = new();
}
