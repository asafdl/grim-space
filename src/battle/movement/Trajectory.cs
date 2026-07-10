using System.Collections.Generic;
using GrimSpace.Battle.Grid;
using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Movement;

public sealed class Trajectory
{
	public required State ExpectedFinalState { get; init; }
	public required List<Coord> Path { get; init; }
}
