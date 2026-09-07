using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Pathfinding;

namespace GrimSpace.World.StarSystem;

public sealed record PendingCourse(Coord Destination, TransitPath Path);
