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
	// The black hole hangs FAR beyond the playfield as a real 3D object
	// (<see cref="MapBackdrop.Geometry"/>: MapBlackHoleVisual), so its numbers
	// are world units, not sky-shader degrees.
	float BlackHoleDistance = 260f,
	float BlackHoleHorizonRadius = 26f,
	float BlackHoleDiskRadius = 50f,
	float SunGlowEnergy = 0.78f,
	float AmbientEnergy = 0.44f,
	float FillLightEnergy = 0.17f)
{
	public Vector3 NebulaDirection { get; init; } = new(0.764f, -0.404f, -0.503f);

	/// <summary>
	/// <see cref="MapBlackHoleVisual"/> placement direction from the map origin;
	/// opposite-ish of <see cref="NebulaDirection"/> so one wide orbit shows both
	/// far layers separately.
	/// </summary>
	public Vector3 BlackHoleDirection { get; init; } = new(-0.62f, -0.38f, -0.52f);

	public static MapAtmosphereSettings Default { get; } = new();
}
