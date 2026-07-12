using GrimSpace.Battle.Actions.Contexts;
using GrimSpace.Battle.Movement.Enums;

namespace GrimSpace.Battle.Actions.Effects;

public sealed class RollEffect(ERollDirection direction) : IStateEffect
{
	public void Apply(OrientationContext orientation) => orientation.Roll(direction);

	void IStateEffect.Apply(ActionSlices slices) => Apply(slices.Orientation);
}
