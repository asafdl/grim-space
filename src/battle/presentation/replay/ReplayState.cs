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

	public bool Contains(string actorId) => _states.ContainsKey(actorId);

	public void Add(State state) => _states[state.Id] = state.Clone();

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

	public void ApplyImpact(ImpactFacts impact)
	{
		if (!_states.TryGetValue(impact.TargetId, out var state))
			return;

		var shield = state.ShieldPoints[impact.Face];
		state.ShieldPoints[impact.Face] = System.Math.Max(0, shield - impact.ShieldDamage);
		state.HullPoints = System.Math.Max(0, state.HullPoints - impact.HullDamage);
		state.MomentumLevel = System.Math.Max(0, state.MomentumLevel - impact.MomentumLoss);
	}
}
