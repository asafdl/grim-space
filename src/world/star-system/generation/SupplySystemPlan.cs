namespace GrimSpace.World.StarSystem.Generation;

using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;

public sealed record SupplySystemPlan(
	string ResourceId,
	string ExtractionPoiId,
	string RefineryPoiId,
	string StoragePoiId,
	string ExitPoiId,
	string AdministrativePoiId)
{
	public const string StarPoiId = "star";
	public const string CopperResourceId = "copper";

	public static SupplySystemPlan Copper { get; } = new(
		CopperResourceId,
		ExtractionPoiId: "poi-extraction",
		RefineryPoiId: "poi-refinery",
		StoragePoiId: "poi-storage",
		ExitPoiId: "poi-exit",
		AdministrativePoiId: "poi-admin");

	public (string FromPoiId, string ToPoiId)[] RouteConnections { get; } =
	[
		(ExtractionPoiId, RefineryPoiId),
		(RefineryPoiId, StoragePoiId),
		(StoragePoiId, ExitPoiId),
		(AdministrativePoiId, ExtractionPoiId),
		(AdministrativePoiId, ExitPoiId),
	];

	public PointOfInterest[] CreatePoiTemplates(int seed) =>
	[
		Star.Template(),
		OreMine.Template(this),
		Refinery.Template(this),
		StorageFacility.Template(this),
		Wormhole.Template(this),
		AdministrativeCore.Template(this, seed),
	];
}
