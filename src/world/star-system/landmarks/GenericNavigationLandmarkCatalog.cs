namespace GrimSpace.World.StarSystem.Landmarks;

public static class GenericNavigationLandmarkCatalog
{
	public static IReadOnlyList<NavigationLandmarkPoolEntry> Entries =>
	[
		new(
			"dust-cloud",
			ENavigationLandmarkKind.DustCloud,
			["The Cinder Drift", "Pale Wake", "Ash Veil"],
			Radius: 52,
			Weight: 1f,
			MaxPerMap: 2),
		new(
			"asteroid-formation",
			ENavigationLandmarkKind.AsteroidFormation,
			["Broken Crown", "Three Sisters", "The Splinter"],
			Radius: 40,
			Weight: 1f,
			MaxPerMap: 2),
		new(
			"moonlet",
			ENavigationLandmarkKind.Moonlet,
			["Morrow", "K-17", "Orphan Rock"],
			Radius: 18,
			Weight: 1f,
			MaxPerMap: 1),
	];
}
