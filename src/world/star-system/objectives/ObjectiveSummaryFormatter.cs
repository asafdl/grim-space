namespace GrimSpace.World.StarSystem.Objectives;

public abstract record ObjectiveSummaryContent
{
	public string ToBbcode() =>
		this switch
		{
			Plain plain => plain.Text,
			NearLandmark near =>
				$"{near.Prefix}{LandmarkLink(near.LandmarkPoiId, near.LandmarkDisplayName)}{near.Suffix}",
			RouteBetweenLandmarks route =>
				$"{route.Prefix}{LandmarkLink(route.LandmarkAPoiId, route.LandmarkADisplayName)}" +
				$"{route.Connector}{LandmarkLink(route.LandmarkBPoiId, route.LandmarkBDisplayName)}{route.Suffix}",
			RouteAmongLandmarks route =>
				$"{route.Prefix}{LandmarkLink(route.LandmarkAPoiId, route.LandmarkADisplayName)}" +
				$"{route.ConnectorAB}{LandmarkLink(route.LandmarkBPoiId, route.LandmarkBDisplayName)}" +
				$"{route.ConnectorBC}{LandmarkLink(route.LandmarkCPoiId, route.LandmarkCDisplayName)}{route.Suffix}",
			_ => throw new ArgumentOutOfRangeException(),
		};

	private static string LandmarkLink(string landmarkId, string displayName) =>
		$"[url=\"{EscapeBbcodeAttribute(landmarkId)}\"]{displayName}[/url]";

	private static string EscapeBbcodeAttribute(string value) =>
		value.Replace("\\", "\\\\", StringComparison.Ordinal)
			.Replace("\"", "\\\"", StringComparison.Ordinal);

	public sealed record Plain(string Text) : ObjectiveSummaryContent;

	public sealed record NearLandmark(
		string Prefix,
		string LandmarkPoiId,
		string LandmarkDisplayName,
		string Suffix) : ObjectiveSummaryContent;

	public sealed record RouteBetweenLandmarks(
		string Prefix,
		string LandmarkAPoiId,
		string LandmarkADisplayName,
		string Connector,
		string LandmarkBPoiId,
		string LandmarkBDisplayName,
		string Suffix) : ObjectiveSummaryContent;

	public sealed record RouteAmongLandmarks(
		string Prefix,
		string LandmarkAPoiId,
		string LandmarkADisplayName,
		string ConnectorAB,
		string LandmarkBPoiId,
		string LandmarkBDisplayName,
		string ConnectorBC,
		string LandmarkCPoiId,
		string LandmarkCDisplayName,
		string Suffix) : ObjectiveSummaryContent;
}
