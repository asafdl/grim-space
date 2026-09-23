using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem.Landmarks;

public sealed record NavigationLandmark(
	string Id,
	string DisplayName,
	ENavigationLandmarkKind Kind,
	Coord Position,
	int Radius,
	int VisualSeed);
