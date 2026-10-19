using Godot;

namespace GrimSpace.Presentation.Graphics;

/// <summary>
/// Colour and tuning for a rendered star.
///
/// The defaults reproduce the real Sun as measured from NASA/SDO reference
/// frames (AIA 304, HMI 193, SOHO/LASCO C2).  The measured values and the
/// frames they came from are documented in <c>SUN_NOTES.md</c>.
/// </summary>
public sealed class SunPalette
{
	/// <summary>Hottest part of the disk, at disc centre.</summary>
	public Color CoreColor { get; init; } = new(1.00f, 0.88f, 0.62f);

	/// <summary>Mid disk, between the core and the limb.</summary>
	public Color MidColor { get; init; } = new(1.00f, 0.42f, 0.10f);

	/// <summary>The limb, where the photosphere cools and reddens.</summary>
	public Color LimbColor { get; init; } = new(0.70f, 0.10f, 0.02f);

	/// <summary>Prominence plasma above the limb.</summary>
	public Color ProminenceColor { get; init; } = new(1.00f, 0.22f, 0.08f);

	/// <summary>Bright chromospheric footpoints where a prominence is rooted.</summary>
	public Color FootpointColor { get; init; } = new(1.00f, 0.48f, 0.15f);

	/// <summary>Chromospheric ring hugging the limb.</summary>
	public Color LimbRingColor { get; init; } = new(1.00f, 0.32f, 0.12f);

	/// <summary>Corona streamers.</summary>
	public Color CoronaColor { get; init; } = new(1.00f, 0.47f, 0.18f);

	/// <summary>Overall brightness of the photosphere.</summary>
	public float EmissionStrength { get; init; } = 0.50f;

	/// <summary>Limb darkening coefficient u of I(mu) = 1 - u(1-mu) - v(1-mu)^2.</summary>
	public float LimbDarkening { get; init; } = 0.50f;

	/// <summary>Limb darkening coefficient v.</summary>
	public float LimbDarkeningSquared { get; init; } = 0.14f;

	/// <summary>How strongly the chromospheric edge steepens the limb.</summary>
	public float LimbEdgeStrength { get; init; } = 0.45f;

	/// <summary>Bright chromospheric rim right at the limb.</summary>
	public Color RimColor { get; init; } = new(1.00f, 0.34f, 0.10f);

	/// <summary>How strong the chromospheric rim is.</summary>
	public float RimStrength { get; init; } = 0.22f;

	/// <summary>Active region (plage) cell count across the visible disk diameter.</summary>
	public float PlageCells { get; init; } = 13f;

	/// <summary>How strong the active regions are.</summary>
	public float PlageStrength { get; init; } = 0.55f;

	/// <summary>Granulation cell count across the visible disk diameter.</summary>
	public float GranulationCells { get; init; } = 176f;

	/// <summary>How strongly the granulation modulates brightness and hue.</summary>
	public float GranulationStrength { get; init; } = 0.85f;

	/// <summary>How many prominence sites are live at once.</summary>
	public float ProminenceCount { get; init; } = 5f;

	/// <summary>Peak prominence height above the limb, in solar radii.</summary>
	public float ProminenceHeight { get; init; } = 0.20f;

	/// <summary>Overall prominence brightness multiplier.</summary>
	public float ProminenceStrength { get; init; } = 1.0f;

	/// <summary>Overall corona brightness multiplier.</summary>
	public float CoronaStrength { get; init; } = 0.10f;

	/// <summary>Deterministic seed for the surface pattern and the prominence sites.</summary>
	public float Seed { get; init; }

	/// <summary>Debug helper: 0 = full, 1 = filament, 2 = curtain, 3 = footpoints.</summary>
	public int ProminenceDebug { get; init; }

	/// <summary>Debug helper for the surface shader: 0 = full, 1..8 = flat grey steps.</summary>
	public int SurfaceDebug { get; init; }

	/// <summary>Width of the prominence billboard, in solar radii.</summary>
	public float ProminenceQuadWidth { get; init; } = 4f;

	/// <summary>Width of the corona billboard, in solar radii.</summary>
	public float CoronaQuadWidth { get; init; } = 6f;
}
