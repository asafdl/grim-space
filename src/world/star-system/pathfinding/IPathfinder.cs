using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem.Pathfinding;

public interface IPathfinder
{
	PathfindingResult FindPath(Coord origin, Coord destination);
}
