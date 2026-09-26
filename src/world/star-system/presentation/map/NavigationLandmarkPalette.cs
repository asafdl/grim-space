using Godot;
using GrimSpace.Math;

namespace GrimSpace.World.StarSystem.Presentation.Map;

internal static class NavigationLandmarkPalette
{
	private static readonly Color[] DustAccents =
	[
		new(0.58f, 0.72f, 0.92f),
		new(0.68f, 0.52f, 0.82f),
		new(0.78f, 0.52f, 0.38f),
		new(0.52f, 0.78f, 0.68f),
		new(0.62f, 0.48f, 0.44f),
	];

	private static readonly Color[] MainRockAccents =
	[
		new(0.82f, 0.51f, 0.29f),
		new(0.66f, 0.38f, 0.31f),
		new(0.78f, 0.69f, 0.37f),
		new(0.38f, 0.62f, 0.55f),
		new(0.43f, 0.53f, 0.72f),
	];

	private static readonly Color[] SupportRockAccents =
	[
		new(0.73f, 0.48f, 0.32f),
		new(0.55f, 0.42f, 0.36f),
		new(0.65f, 0.61f, 0.39f),
		new(0.39f, 0.55f, 0.53f),
	];

	public static Color DustCardColor(int visualSeed, int cardIndex, RandomNumberGenerator random, float alpha)
	{
		var palette = (int)(StableSeedMixer.From(visualSeed).Add("dust-palette").Value % (ulong)DustAccents.Length);
		var accent = DustAccents[palette];
		var jitter = (int)(random.Randi() % (uint)DustAccents.Length);
		var variant = DustAccents[Mathf.PosMod(palette + cardIndex + jitter, DustAccents.Length)];
		var mixed = accent.Lerp(variant, 0.35f + random.Randf() * 0.25f);
		var lift = 0.88f + random.Randf() * 0.18f;
		return new Color(mixed.R * lift, mixed.G * lift, mixed.B * lift, alpha);
	}

	public static Color MainRockAccent(RandomNumberGenerator random) =>
		MainRockAccents[(int)(random.Randi() % (uint)MainRockAccents.Length)];

	public static Color SupportRockAccent(RandomNumberGenerator random) =>
		SupportRockAccents[(int)(random.Randi() % (uint)SupportRockAccents.Length)];
}
