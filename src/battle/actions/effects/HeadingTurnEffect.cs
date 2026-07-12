using GrimSpace.Battle.Actions.Contexts;
using GrimSpace.Battle.Movement.Enums;

namespace GrimSpace.Battle.Actions.Effects;

public sealed class HeadingTurnEffect(EHeadingTurn turn) : IStateEffect
{
	public void Apply(OrientationContext orientation) => orientation.Turn(turn);

	void IStateEffect.Apply(ActionSlices slices) => Apply(slices.Orientation);
}
