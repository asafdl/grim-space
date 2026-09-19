namespace GrimSpace.World.StarSystem.Areas;

public static class AreaIntelDisplay
{
	private const string PlaceholderA = "{A}";
	private const string PlaceholderB = "{B}";

	public sealed record LinkableSegments(
		string Prefix,
		string LandmarkAId,
		string Connector,
		string LandmarkBId,
		string Suffix);

	public static string FormatPlain(AreaIntel intel, Func<string, string?> resolveDisplayName)
	{
		ArgumentNullException.ThrowIfNull(intel);
		ArgumentNullException.ThrowIfNull(resolveDisplayName);

		var nameA = resolveDisplayName(intel.LandmarkAId) ?? intel.LandmarkAId;
		var nameB = resolveDisplayName(intel.LandmarkBId) ?? intel.LandmarkBId;
		return intel.Template
			.Replace(PlaceholderA, nameA, StringComparison.Ordinal)
			.Replace(PlaceholderB, nameB, StringComparison.Ordinal);
	}

	public static bool TryParseLinkableSegments(AreaIntel intel, out LinkableSegments segments)
	{
		ArgumentNullException.ThrowIfNull(intel);

		var template = intel.Template;
		var indexA = template.IndexOf(PlaceholderA, StringComparison.Ordinal);
		var indexB = template.IndexOf(PlaceholderB, StringComparison.Ordinal);
		if (indexA < 0 || indexB < 0 || indexA >= indexB)
		{
			segments = default!;
			return false;
		}

		segments = new LinkableSegments(
			template[..indexA],
			intel.LandmarkAId,
			template[(indexA + PlaceholderA.Length)..indexB],
			intel.LandmarkBId,
			template[(indexB + PlaceholderB.Length)..]);
		return true;
	}
}
