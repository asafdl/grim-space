namespace GrimSpace.World.StarSystem.Landmarks;

public static class CopperNavigationLandmarkCatalog
{
	public static IReadOnlyList<NavigationLandmarkPoolEntry> Entries =>
	[
		new(
			"tailings-field",
			ENavigationLandmarkKind.TailingsField,
			["Refinery Tailings 4", "Red Slag Reach", "Spent Vein"],
			Radius: 54,
			Weight: 1f,
			MaxPerMap: 2),
	];
}
