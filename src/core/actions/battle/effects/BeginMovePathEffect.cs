using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle.Effects;

public sealed class BeginMovePathEffect : IEffect<BattleSlices>
{
	public void Apply(State actor, TurnState turnState) => turnState.ResetMovePath(actor.MomentumLevel);

	void IEffect<BattleSlices>.Apply(BattleSlices slices) =>
		Apply(slices.Ap.Player, slices.TurnState);
}
