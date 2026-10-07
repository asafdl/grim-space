using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;

namespace GrimSpace.Run.Persistence;

public sealed record HazardSaveDto(
	string Id,
	string ActorId,
	Coord Center,
	BodyFrame Frame,
	IReadOnlyList<Coord> Cells);
