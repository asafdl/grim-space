namespace GrimSpace.World.StarSystem.Presentation;

public static class MapNavigationContext
{
	public const string MapScenePath = "res://scenes/map.tscn";

	public static string? ActivePoiId { get; private set; }

	public static string? ActiveFacilityId { get; private set; }

	public static bool ReturnToFacade { get; private set; }

	public static void EnterFacility(string poiId, string facilityId)
	{
		ActivePoiId = poiId;
		ActiveFacilityId = facilityId;
		ReturnToFacade = true;
	}

	public static void ClearReturnToFacade() => ReturnToFacade = false;
}
