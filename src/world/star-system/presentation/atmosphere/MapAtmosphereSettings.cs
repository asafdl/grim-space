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
	float HorizonStrength = 0.36f,
	float HorizonAngularRadius = 0.12f,
	float HorizonAspect = 1.778f,
	float SunGlowEnergy = 0.78f,
	float AmbientEnergy = 0.13f,
	float DustDensity = 0.7f,
	float DustOpacity = 0.11f)
{
	public Vector3 NebulaDirection { get; init; } = new(0.764f, -0.404f, -0.503f);

	/// <summary>Same sky band as <see cref="NebulaDirection"/> (negative Y) so top-down orbit camera sees both.</summary>
	public Vector3 HorizonDirection { get; init; } = new(-0.62f, -0.38f, -0.52f);

	public static MapAtmosphereSettings Default { get; } = new();
}
