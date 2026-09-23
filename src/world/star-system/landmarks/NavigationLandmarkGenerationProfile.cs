namespace GrimSpace.World.StarSystem.Landmarks;

public sealed record NavigationLandmarkGenerationProfile(
	int MinimumCount,
	int TargetCount,
	int MaximumCount,
	int EdgePadding,
	int PoiClearance,
	int RouteClearance,
	int LandmarkSeparation,
	int CandidateSampleCount,
	IReadOnlyList<NavigationLandmarkPoolEntry> GenericPool,
	IReadOnlyList<NavigationLandmarkPoolEntry> SystemSpecificPool)
{
	public static NavigationLandmarkGenerationProfile Disabled =>
		new(
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			[],
			[]);

	public static NavigationLandmarkGenerationProfile DefaultSupply =>
		new(
			MinimumCount: 5,
			TargetCount: 7,
			MaximumCount: 8,
			EdgePadding: 48,
			PoiClearance: 24,
			RouteClearance: 16,
			LandmarkSeparation: 40,
			CandidateSampleCount: 64,
			GenericPool: GenericNavigationLandmarkCatalog.Entries,
			SystemSpecificPool: CopperNavigationLandmarkCatalog.Entries);
}
