namespace GrimSpace.World.StarSystem.Generation;

public sealed record SupplySystemPlan(
	string ResourceId,
	string ExtractionPoiId,
	string RefineryPoiId,
	string StoragePoiId,
	string ExitPoiId)
{
	public const string StarPoiId = "star";
	public const string CopperResourceId = "copper";

	public static SupplySystemPlan Copper { get; } = new(
		CopperResourceId,
		ExtractionPoiId: "poi-extraction",
		RefineryPoiId: "poi-refinery",
		StoragePoiId: "poi-storage",
		ExitPoiId: "poi-exit");

	public (string FromPoiId, string ToPoiId)[] RouteConnections { get; } =
	[
		(ExtractionPoiId, RefineryPoiId),
		(RefineryPoiId, StoragePoiId),
		(StoragePoiId, ExitPoiId),
	];
}
