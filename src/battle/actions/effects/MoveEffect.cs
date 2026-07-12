using GrimSpace.Battle.Actions.Contexts;
using GrimSpace.Battle.Movement;

namespace GrimSpace.Battle.Actions.Effects;

public sealed class MoveEffect(Option option) : IStateEffect
{
	public void Apply(MoveContext move) => move.Apply(option);

	void IStateEffect.Apply(ActionSlices slices) => Apply(slices.Move);
}
