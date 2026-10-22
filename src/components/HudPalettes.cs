using Godot;

namespace GrimSpace.Components;

public readonly record struct HudPalette(
	Color Surface,
	Color DeepSurface,
	Color Border,
	Color Title,
	Color Primary,
	Color Secondary,
	Color Highlight);

public static class HudPalettes
{
	public static readonly HudPalette BlueCyan = new(
		Surface: new Color(0.02f, 0.04f, 0.07f), // hex #050814
		DeepSurface: new Color(0.025f, 0.025f, 0.045f), // hex #040712
		Border: new Color(0.22f, 0.32f, 0.42f), // hex #36526A
		Title: new Color(0.98f, 0.82f, 0.14f), // hex #F8D426
		Primary: new Color(0.55f, 0.78f, 1f), // hex #8CC4FF
		Secondary: new Color(0.48f, 0.82f, 0.82f), // hex #7CC6C6
		Highlight: new Color(0.3f, 0.66f, 1f)); // hex #4DAAFF

	public static readonly HudPalette Gold = new(
		Surface: new Color(0.1f, 0.075f, 0.025f), // hex #191206
		DeepSurface: new Color(0.045f, 0.03f, 0.01f), // hex #0B0703
		Border: new Color(0.55f, 0.42f, 0.2f), // hex #8C6A32
		Title: new Color(0.98f, 0.82f, 0.14f), // hex #F8D426
		Primary: new Color(0.98f, 0.82f, 0.14f), // hex #F8D426
		Secondary: new Color(1f, 0.78f, 0.3f), // hex #FFC84D
		Highlight: new Color(1f, 0.9f, 0.55f)); // hex #FFE68C

	public static readonly HudPalette PurpleRed = new(
		Surface: new Color(0.11f, 0.08f, 0.14f), // hex #1C1224
		DeepSurface: new Color(0.025f, 0.025f, 0.045f), // hex #040712
		Border: new Color(0.38f, 0.29f, 0.44f), // hex #604A70
		Title: new Color(0.88f, 0.32f, 0.42f), // hex #E4526C
		Primary: new Color(0.72f, 0.63f, 0.76f), // hex #B89FBF
		Secondary: new Color(0.52f, 0.45f, 0.7f), // hex #8672B2
		Highlight: new Color(0.88f, 0.32f, 0.42f)); // hex #E4526C

	public static Color WithAlpha(Color color, float alpha) =>
		new(color.R, color.G, color.B, alpha);
}
