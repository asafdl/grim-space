namespace GrimSpace.World.StarSystem.Landmarks;

public sealed record NavigationLandmarkPoolEntry(
	string Slug,
	ENavigationLandmarkKind Kind,
	IReadOnlyList<string> Names,
	int Radius,
	float Weight,
	int MaxPerMap);
