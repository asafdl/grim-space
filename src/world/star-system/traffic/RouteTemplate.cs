namespace GrimSpace.World.StarSystem.Traffic;

public sealed record RouteTemplate(
	string Id,
	string OriginPoiId,
	string DestinationPoiId,
	string OriginDockId,
	string DestinationDockId,
	IReadOnlyList<string> SegmentIds);
