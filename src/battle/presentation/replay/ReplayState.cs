using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Replay;

public sealed class ReplayState
{
	private readonly Dictionary<string, State> _states;

	public ReplayState(IReadOnlyDictionary<string, State> turnStart)
	{
		_states = turnStart.ToDictionary(pair => pair.Key, pair => pair.Value.Clone());
	}

	public State StateOf(string actorId) => _states[actorId];

	public Coord Position(string actorId) => _states[actorId].Position;

	public void ApplyHeadingTurn(HeadingTurnAction turn) =>
		Orientation.ApplyHeadingTurn(_states[turn.ActorId], turn.Turn);

	public void ApplyRoll(RollAction roll) =>
		Orientation.ApplyRoll(_states[roll.ActorId], roll.Direction);

	public void ApplyMove(MoveStepAction move)
	{
		var state = _states[move.ActorId];
		var frame = BodyFrame.From(state);
		state.Position += frame.Step(move.Direction);
	}
}
