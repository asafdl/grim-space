namespace GrimSpace.World.StarSystem.Objectives;

public abstract record ObjectiveSummaryContent
{
	public sealed record Plain(string Text) : ObjectiveSummaryContent;

	public sealed record RouteBetweenLandmarks(
		string Prefix,
		string LandmarkAPoiId,
		string LandmarkADisplayName,
		string Connector,
		string LandmarkBPoiId,
		string LandmarkBDisplayName,
		string Suffix) : ObjectiveSummaryContent;
}
