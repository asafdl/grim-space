namespace GrimSpace.World.StarSystem.Areas;

public static class AreaIntelDisplay
{
	private const string PlaceholderA = "{A}";
	private const string PlaceholderB = "{B}";
	private const string PlaceholderC = "{C}";

	public abstract record ParsedSegments
	{
		public sealed record ClosestOnly(
			string Prefix,
			string LandmarkAId,
			string Suffix) : ParsedSegments;

		public sealed record ClosestAndSecondary(
			string Prefix,
			string LandmarkAId,
			string Connector,
			string LandmarkBId,
			string Suffix) : ParsedSegments;

		public sealed record ClosestSecondaryAndAnchor(
			string Prefix,
			string LandmarkAId,
			string ConnectorAB,
			string LandmarkBId,
			string ConnectorBC,
			string LandmarkCId,
			string Suffix) : ParsedSegments;
	}

	public static string FormatPlain(AreaIntel intel, Func<string, string?> resolveDisplayName)
	{
		ArgumentNullException.ThrowIfNull(intel);
		ArgumentNullException.ThrowIfNull(resolveDisplayName);

		var nameA = resolveDisplayName(intel.LandmarkAId) ?? intel.LandmarkAId;
		var nameB = resolveDisplayName(intel.LandmarkBId) ?? intel.LandmarkBId;
		var nameC = resolveDisplayName(intel.LandmarkCId) ?? intel.LandmarkCId;
		return intel.Template
			.Replace(PlaceholderA, nameA, StringComparison.Ordinal)
			.Replace(PlaceholderB, nameB, StringComparison.Ordinal)
			.Replace(PlaceholderC, nameC, StringComparison.Ordinal);
	}

	public static bool TryParseLinkableSegments(AreaIntel intel, out ParsedSegments segments)
	{
		ArgumentNullException.ThrowIfNull(intel);

		var template = intel.Template;
		var indexA = template.IndexOf(PlaceholderA, StringComparison.Ordinal);
		if (indexA < 0)
		{
			segments = default!;
			return false;
		}

		var indexB = template.IndexOf(PlaceholderB, StringComparison.Ordinal);
		var indexC = template.IndexOf(PlaceholderC, StringComparison.Ordinal);

		if (indexB < 0)
		{
			segments = new ParsedSegments.ClosestOnly(
				template[..indexA],
				intel.LandmarkAId,
				template[(indexA + PlaceholderA.Length)..]);
			return true;
		}

		if (indexA >= indexB)
		{
			segments = default!;
			return false;
		}

		if (indexC < 0)
		{
			segments = new ParsedSegments.ClosestAndSecondary(
				template[..indexA],
				intel.LandmarkAId,
				template[(indexA + PlaceholderA.Length)..indexB],
				intel.LandmarkBId,
				template[(indexB + PlaceholderB.Length)..]);
			return true;
		}

		if (indexB >= indexC)
		{
			segments = default!;
			return false;
		}

		segments = new ParsedSegments.ClosestSecondaryAndAnchor(
			template[..indexA],
			intel.LandmarkAId,
			template[(indexA + PlaceholderA.Length)..indexB],
			intel.LandmarkBId,
			template[(indexB + PlaceholderB.Length)..indexC],
			intel.LandmarkCId,
			template[(indexC + PlaceholderC.Length)..]);
		return true;
	}
}
