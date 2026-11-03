using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public readonly record struct MovedFacts(
	string ActorId,
	Coord From,
	Coord To);
