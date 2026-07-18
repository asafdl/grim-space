using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle.Effects;

public sealed class MoveStepMomentumEffect(EStepDirection direction) : IEffect<BattleSlices>
{
	public void Apply(State actor, TurnState turnState)
	{
		var moveStart = turnState.MoveStartMomentumLevel;
		var buildup = MomentumConfig.ApplyMovementStep(
			turnState.MovementBuildup,
			direction,
			moveStart,
			turnState.MomentumGainedFromMovementThisTurn);
		turnState.SetMovementBuildup(buildup);
		actor.MomentumLevel = buildup.Level;
	}

	void IEffect<BattleSlices>.Apply(BattleSlices slices) =>
		Apply(slices.Ap.Player, slices.TurnState);
}
