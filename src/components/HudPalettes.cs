using Godot;

namespace GrimSpace.Components;

public readonly record struct HudPalette(
	Color Surface,
	Color DeepSurface,
	Color Border,
	Color Primary,
	Color Secondary,
	Color Highlight);

public static class HudPalettes
{
	public static readonly HudPalette BlueCyan = new(
		Surface: new Color(0.02f, 0.04f, 0.07f),
		DeepSurface: new Color(0.025f, 0.025f, 0.045f),
		Border: new Color(0.22f, 0.32f, 0.42f),
		Primary: new Color(0.55f, 0.78f, 1f),
		Secondary: new Color(0.48f, 0.82f, 0.82f),
		Highlight: new Color(0.3f, 0.66f, 1f));

	public static readonly HudPalette Gold = new(
		Surface: new Color(0.1f, 0.075f, 0.025f),
		DeepSurface: new Color(0.045f, 0.03f, 0.01f),
		Border: new Color(0.55f, 0.42f, 0.2f),
		Primary: new Color(0.98f, 0.82f, 0.14f),
		Secondary: new Color(1f, 0.78f, 0.3f),
		Highlight: new Color(1f, 0.9f, 0.55f));

	public static readonly HudPalette PurpleRed = new(
		Surface: new Color(0.11f, 0.08f, 0.14f),
		DeepSurface: new Color(0.025f, 0.025f, 0.045f),
		Border: new Color(0.38f, 0.29f, 0.44f),
		Primary: new Color(0.72f, 0.63f, 0.76f),
		Secondary: new Color(0.52f, 0.45f, 0.7f),
		Highlight: new Color(0.88f, 0.32f, 0.42f));

	public static Color WithAlpha(Color color, float alpha) =>
		new(color.R, color.G, color.B, alpha);
}
