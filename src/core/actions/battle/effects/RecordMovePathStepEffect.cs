using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle.Effects;

public sealed class RecordMovePathStepEffect(EStepDirection direction, int directionBit) : IEffect<BattleSlices>
{
	public void Apply(TurnState turnState) => turnState.RecordMoveStep(direction, directionBit);

	void IEffect<BattleSlices>.Apply(BattleSlices slices) => Apply(slices.TurnState);
}
