using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle.Effects;

public sealed class HeadingTurnEffect(EHeadingTurn turn) : IEffect<BattleSlices>
{
	public void Apply(OrientationContext orientation) => orientation.Turn(turn);

	void IEffect<BattleSlices>.Apply(BattleSlices slices) => Apply(slices.Orientation);
}
