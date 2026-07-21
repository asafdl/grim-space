using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Battle.Effects;

public sealed class RollEffect(ERollDirection direction) : IEffect<BattleSlices>
{
	public void Apply(OrientationContext orientation) => orientation.Roll(direction);

	void IEffect<BattleSlices>.Apply(BattleSlices slices) => Apply(slices.Orientation);
}
