namespace GrimSpace.World.StarSystem.Objectives;

public static class ObjectiveSummaryFormatter
{
	public static string ToBbcode(ObjectiveSummaryContent summary) =>
		summary switch
		{
			ObjectiveSummaryContent.Plain plain => plain.Text,
			ObjectiveSummaryContent.RouteBetweenLandmarks route =>
				$"{route.Prefix}[url={route.LandmarkAPoiId}]{route.LandmarkADisplayName}[/url]" +
				$"{route.Connector}[url={route.LandmarkBPoiId}]{route.LandmarkBDisplayName}[/url]{route.Suffix}",
			_ => throw new ArgumentOutOfRangeException(nameof(summary)),
		};
}
