using System.Globalization;

namespace GrimSpace.Pilots;

public static class PilotPortraitId
{
	public const string Default = "robot_v1:0";
	private const string Prefix = "robot_v1:";

	public static string Roll(Random random)
	{
		ArgumentNullException.ThrowIfNull(random);
		return Prefix + random.Next().ToString(CultureInfo.InvariantCulture);
	}

	public static int SeedFor(string portraitId)
	{
		ArgumentException.ThrowIfNullOrEmpty(portraitId);
		if (portraitId.StartsWith(Prefix, StringComparison.Ordinal)
			&& int.TryParse(portraitId.AsSpan(Prefix.Length), NumberStyles.None,
				CultureInfo.InvariantCulture, out var seed))
			return seed;

		if (portraitId.Length == 8
			&& portraitId.StartsWith("pilot_", StringComparison.Ordinal)
			&& int.TryParse(portraitId.AsSpan(6), NumberStyles.None,
				CultureInfo.InvariantCulture, out var legacyIndex)
			&& legacyIndex is >= 1 and <= 10)
			return legacyIndex;

		throw new ArgumentException($"Unknown pilot portrait '{portraitId}'.", nameof(portraitId));
	}
}
