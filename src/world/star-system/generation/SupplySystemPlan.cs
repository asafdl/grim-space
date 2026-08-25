namespace GrimSpace.World.StarSystem.Generation;

using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;

public sealed record SupplySystemPlan(
	string ResourceId,
	string ExtractionPoiId,
	string RefineryPoiId,
	string StoragePoiId,
	string ExitPoiId,
	string AdministrativePoiId,
	string TradeHubPoiId)
{
	public const string StarPoiId = "star";
	public const string CopperResourceId = "copper";

	public static SupplySystemPlan Copper { get; } = new(
		CopperResourceId,
		ExtractionPoiId: "poi-extraction",
		RefineryPoiId: "poi-refinery",
		StoragePoiId: "poi-storage",
		ExitPoiId: "poi-exit",
		AdministrativePoiId: "poi-admin",
		TradeHubPoiId: "poi-trade");

	public string[] OperationalPoiIds { get; } =
	[
		ExtractionPoiId,
		RefineryPoiId,
		StoragePoiId,
		ExitPoiId,
		AdministrativePoiId,
		TradeHubPoiId,
	];

	public (string FromPoiId, string ToPoiId)[] RouteConnections { get; } =
	[
		(ExtractionPoiId, RefineryPoiId),
		(RefineryPoiId, StoragePoiId),
		(StoragePoiId, ExitPoiId),
		(AdministrativePoiId, ExtractionPoiId),
		(AdministrativePoiId, ExitPoiId),
		(TradeHubPoiId, RefineryPoiId),
		(TradeHubPoiId, StoragePoiId),
		(TradeHubPoiId, ExitPoiId),
	];

	public bool HasRoute(string fromPoiId, string toPoiId)
	{
		foreach (var (from, to) in RouteConnections)
		{
			if ((from == fromPoiId && to == toPoiId) || (from == toPoiId && to == fromPoiId))
				return true;
		}

		return false;
	}

	public PointOfInterest[] CreatePoiTemplates(int seed) =>
	[
		Star.Template(),
		OreMine.Template(this),
		Refinery.Template(this),
		StorageFacility.Template(this),
		Wormhole.Template(this),
		AdministrativeCore.Template(this, seed),
		TradeHub.Template(this),
	];
}
