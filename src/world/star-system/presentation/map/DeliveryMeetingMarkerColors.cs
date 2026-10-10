using GrimSpace.Math;

namespace GrimSpace.World.StarSystem.Presentation.Map;

public static class DeliveryMeetingMarkerColors
{
	public readonly record struct Rgb(float R, float G, float B);

	private const float SimilarityThresholdSquared = 0.045f;

	private static readonly Rgb[] Palette =
	[
		new(0.98f, 0.38f, 0.24f),
		new(0.98f, 0.78f, 0.22f),
		new(0.64f, 0.42f, 0.98f),
		new(0.98f, 0.34f, 0.72f),
		new(0.92f, 0.52f, 0.08f),
		new(0.78f, 0.22f, 0.92f),
	];

	public static Rgb Pick(int mapSeed, string meetingId, Rgb hullColor, Rgb playerColor)
	{
		var available = Palette
			.Where(color => !IsTooSimilar(color, hullColor) && !IsTooSimilar(color, playerColor))
			.ToArray();
		if (available.Length == 0)
			available = Palette;

		var seed = StableSeedMixer.From(mapSeed)
			.Add("delivery-meeting-ring")
			.Add(meetingId)
			.Value;
		return available[(int)(seed % (ulong)available.Length)];
	}

	private static bool IsTooSimilar(Rgb a, Rgb b)
	{
		var dr = a.R - b.R;
		var dg = a.G - b.G;
		var db = a.B - b.B;
		return dr * dr + dg * dg + db * db < SimilarityThresholdSquared;
	}
}
