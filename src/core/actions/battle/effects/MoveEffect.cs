using GrimSpace.Battle.Movement;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle.Effects;

public sealed class MoveEffect(Option option) : IEffect<BattleSlices>
{
	public void Apply(MoveContext move) => move.Apply(option);

	void IEffect<BattleSlices>.Apply(BattleSlices slices) => Apply(slices.Move);
}
