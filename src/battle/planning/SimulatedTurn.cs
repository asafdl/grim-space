using GrimSpace.Battle.Board;
using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Planning;

public sealed class SimulatedTurn
{
	public required BattleBoard Board { get; init; }
	public required string ActorId { get; init; }

	public State Actor => Board.StateOf(ActorId);

	public IEnumerable<Hazard> Hazards => Board.TurnHazards;
}
