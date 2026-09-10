namespace GrimSpace.Components;

/// <summary>
/// Presentation context for HUD surfaces — not interchangeable skins.
/// All contexts share the ship-systems palette; density and typography vary by use.
/// </summary>
public enum HudThemeFamily
{
	Theatrical,
	Informative,
	Battle,
	Debug,
}
