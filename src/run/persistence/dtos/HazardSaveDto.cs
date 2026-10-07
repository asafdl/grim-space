using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;

namespace GrimSpace.Run.Persistence;

public sealed record HazardSaveDto(
	string Id,
	string ActorId,
	Coord Center,
	BodyFrame Frame,
	IReadOnlyList<Coord> Cells,
	bool Passable,
	bool? BlocksAbilities,
	EHazardKind Kind);
