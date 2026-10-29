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
	// 0.0996 / 0.190 rad. SKYPASS V3P1 ITEM S — whole-feature scale down by
	// 0.60: the rendered envelope measured ~71% of frame height at the
	// standard mappose and the user ruled 40-45%; every BH length carries the
	// same factor (shadow 0.10 -> 0.06, glow 0.19 -> 0.114, plus the ring
	// thickness uniform in map_sky.gdshader 0.105 -> 0.063) so the internal
	// proportions are unchanged — only the on-screen size moves. The shader's
	// own uniform defaults are kept in sync as fallbacks.
	float BlackHoleShadowAngularRadius = 0.06f,
	// SKYPASS V3P2 — glow 0.114 -> 0.126 only for early-out headroom above the
	// repaired crown band (crest 1.9x radius) and the broadened under-bowl;
	// the on-screen crisp size (shadow 0.06) is untouched, matching the
	// shader's uniform default (kept in sync as a fallback).
	float BlackHoleGlowAngularRadius = 0.126f,
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
