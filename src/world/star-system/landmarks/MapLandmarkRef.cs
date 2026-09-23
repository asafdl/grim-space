using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem.Landmarks;

public sealed record MapLandmarkRef(
	string Id,
	string DisplayName,
	Coord Position,
	int Radius,
	EMapLandmarkSource Source);
