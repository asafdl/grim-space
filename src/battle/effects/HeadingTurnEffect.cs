using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Battle.Effects;

public sealed class HeadingTurnEffect(EHeadingTurn turn) : IEffect<BattleSlices>
{
	public void Apply(OrientationContext orientation) => orientation.Turn(turn);

	void IEffect<BattleSlices>.Apply(BattleSlices slices) => Apply(slices.Orientation);
}
