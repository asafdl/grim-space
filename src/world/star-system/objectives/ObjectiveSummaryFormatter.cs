namespace GrimSpace.World.StarSystem.Objectives;

public abstract record ObjectiveSummaryContent
{
	public string ToBbcode() =>
		this switch
		{
			Plain plain => plain.Text,
			NearLandmark near =>
				$"{near.Prefix}[url={near.LandmarkPoiId}]{near.LandmarkDisplayName}[/url]{near.Suffix}",
			RouteBetweenLandmarks route =>
				$"{route.Prefix}[url={route.LandmarkAPoiId}]{route.LandmarkADisplayName}[/url]" +
				$"{route.Connector}[url={route.LandmarkBPoiId}]{route.LandmarkBDisplayName}[/url]{route.Suffix}",
			RouteAmongLandmarks route =>
				$"{route.Prefix}[url={route.LandmarkAPoiId}]{route.LandmarkADisplayName}[/url]" +
				$"{route.ConnectorAB}[url={route.LandmarkBPoiId}]{route.LandmarkBDisplayName}[/url]" +
				$"{route.ConnectorBC}[url={route.LandmarkCPoiId}]{route.LandmarkCDisplayName}[/url]{route.Suffix}",
			_ => throw new ArgumentOutOfRangeException(),
		};

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
