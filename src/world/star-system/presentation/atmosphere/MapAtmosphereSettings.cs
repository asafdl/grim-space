namespace GrimSpace.World.StarSystem.Presentation.Atmosphere;

/// <summary>
/// Tunable map atmosphere controls. Defaults target a quiet, expansive system view
/// that stays visually distinct from battle's enclosed chamber.
/// </summary>
public sealed record MapAtmosphereSettings(
	float StarfieldEnergy = 0.26f,
	float SunGlowEnergy = 0.78f,
	float AmbientEnergy = 0.13f,
	float DustDensity = 0.7f,
	float DustOpacity = 0.11f)
{
	public static MapAtmosphereSettings Default { get; } = new();
}
