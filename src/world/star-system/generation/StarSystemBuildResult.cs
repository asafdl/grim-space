namespace GrimSpace.World.StarSystem.Generation;

public sealed record StarSystemBuildResult(
	StarMap Map,
	Pathfinding.PathfindingTerrain Terrain);
