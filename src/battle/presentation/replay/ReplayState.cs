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
		var basis = GridBasis.From(state.Fore, state.Dorsal, state.Starboard);
		var headingBasis = move.Heading is { } heading
			? Orientation.HeadingTurn(basis, heading)
			: basis;
		state.Position += headingBasis.Forward;
		var arrivalBasis = move.Roll is { } roll
			? Orientation.Roll(headingBasis, roll)
			: headingBasis;
		state.Fore = arrivalBasis.Forward;
		state.Dorsal = arrivalBasis.Up;
		state.Starboard = arrivalBasis.Right;
	}

	public void ApplyTorpedoMove(TorpedoMoveStepAction move)
	{
		var state = _states[move.ActorId];
		state.Position += BodyFrame.From(state).Step(move.Direction);
	}

	public void ApplyMomentum(MomentumChangedFacts momentum) =>
		_states[momentum.ActorId].MomentumLevel = momentum.MomentumLevel;

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
