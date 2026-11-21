using System.Globalization;
using GrimSpace.Ui.Portraits;

namespace GrimSpace.Pilots;

public static class PilotPortraitId
{
	public const string Default = "robot_v1:0";
	private const string Prefix = "robot_v1:";
	private const string PaletteMarker = ":p";

	public static string Roll(Random random)
	{
		ArgumentNullException.ThrowIfNull(random);
		return Prefix + random.Next().ToString(CultureInfo.InvariantCulture);
	}

	public static int SeedFor(string portraitId)
	{
		ArgumentException.ThrowIfNullOrEmpty(portraitId);
		if (TryParse(portraitId, out var seed, out _))
			return seed;

		if (portraitId.Length == 8
			&& portraitId.StartsWith("pilot_", StringComparison.Ordinal)
			&& int.TryParse(portraitId.AsSpan(6), NumberStyles.None,
				CultureInfo.InvariantCulture, out var legacyIndex)
			&& legacyIndex is >= 1 and <= 10)
			return legacyIndex;

		throw new ArgumentException($"Unknown pilot portrait '{portraitId}'.", nameof(portraitId));
	}

	public static bool TryGetPaletteIndex(string portraitId, out int paletteIndex)
	{
		paletteIndex = -1;
		return TryParse(portraitId, out _, out paletteIndex) && paletteIndex >= 0;
	}

	public static string WithPalette(string portraitId, int paletteIndex)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(paletteIndex);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(
			paletteIndex,
			RobotPortraitGenerator.Palettes.Count);
		var seed = SeedFor(portraitId);
		return Format(seed, paletteIndex);
	}

	public static string WithRandomOtherPalette(string portraitId, Random random)
	{
		ArgumentNullException.ThrowIfNull(random);
		var seed = SeedFor(portraitId);
		var current = ResolvePaletteIndex(portraitId, seed);
		var count = RobotPortraitGenerator.Palettes.Count;
		if (count <= 1)
			return Format(seed, current);

		var next = random.Next(count - 1);
		if (next >= current)
			next++;
		return Format(seed, next);
	}

	public static GeneratedRobotPortrait GenerateAppearance(string portraitId)
	{
		var seed = SeedFor(portraitId);
		var generated = RobotPortraitGenerator.Generate(seed);
		if (!TryGetPaletteIndex(portraitId, out var paletteIndex))
			return generated;

		return generated with { Colors = RobotPortraitGenerator.Palettes[paletteIndex].Colors };
	}

	public static int ResolvePaletteIndex(string portraitId, int? seed = null)
	{
		if (TryGetPaletteIndex(portraitId, out var paletteIndex))
			return paletteIndex;

		var resolvedSeed = seed ?? SeedFor(portraitId);
		var colors = RobotPortraitGenerator.Generate(resolvedSeed).Colors;
		for (var index = 0; index < RobotPortraitGenerator.Palettes.Count; index++)
		{
			if (RobotPortraitGenerator.Palettes[index].Colors.Equals(colors))
				return index;
		}

		return 0;
	}

	private static string Format(int seed, int paletteIndex) =>
		Prefix
		+ seed.ToString(CultureInfo.InvariantCulture)
		+ PaletteMarker
		+ paletteIndex.ToString(CultureInfo.InvariantCulture);

	private static bool TryParse(string portraitId, out int seed, out int paletteIndex)
	{
		seed = 0;
		paletteIndex = -1;
		if (!portraitId.StartsWith(Prefix, StringComparison.Ordinal))
			return false;

		var body = portraitId.AsSpan(Prefix.Length);
		var paletteAt = body.IndexOf(PaletteMarker, StringComparison.Ordinal);
		ReadOnlySpan<char> seedSpan;
		if (paletteAt < 0)
		{
			seedSpan = body;
		}
		else
		{
			seedSpan = body[..paletteAt];
			var paletteSpan = body[(paletteAt + PaletteMarker.Length)..];
			if (!int.TryParse(paletteSpan, NumberStyles.None, CultureInfo.InvariantCulture, out paletteIndex)
				|| paletteIndex < 0
				|| paletteIndex >= RobotPortraitGenerator.Palettes.Count)
				return false;
		}

		return int.TryParse(seedSpan, NumberStyles.None, CultureInfo.InvariantCulture, out seed);
	}
}
