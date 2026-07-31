using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
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

	public IReadOnlyList<string> ApplyResolveHazard(ResolveHazardAction action)
	{
		var hitUnits = _states.Values
			.Where(unit => unit.IsAlive && action.Cells.Contains(unit.Position))
			.Select(unit => unit.Id)
			.ToList();

		if (hitUnits.Count == 0)
			return hitUnits;

		var shooterPosition = _states[action.ActorId].Position;
		HazardResolution.ApplyScheduledResolve(
			action.Kind,
			action.Cells,
			action.Damage,
			action.MomentumLoss,
			shooterPosition,
			action.ActorId,
			_states.Values);

		return hitUnits;
	}
}
