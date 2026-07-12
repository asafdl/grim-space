using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle.Effects;

public sealed class RollEffect(ERollDirection direction) : IEffect<BattleSlices>
{
	public void Apply(OrientationContext orientation) => orientation.Roll(direction);

	void IEffect<BattleSlices>.Apply(BattleSlices slices) => Apply(slices.Orientation);
}
