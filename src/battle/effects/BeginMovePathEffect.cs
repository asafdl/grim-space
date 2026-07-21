using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Battle.Effects;

public sealed class BeginMovePathEffect : IEffect<BattleSlices>
{
	public void Apply(State actor, TurnState turnState) => turnState.ResetMovePath(actor.MomentumLevel);

	void IEffect<BattleSlices>.Apply(BattleSlices slices) =>
		Apply(slices.Ap.Player, slices.TurnState);
}
