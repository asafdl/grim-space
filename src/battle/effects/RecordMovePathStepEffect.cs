using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Battle.Effects;

public sealed class RecordMovePathStepEffect(EStepDirection direction, int directionBit) : IEffect<BattleSlices>
{
	public void Apply(TurnState turnState) => turnState.RecordMoveStep(direction, directionBit);

	void IEffect<BattleSlices>.Apply(BattleSlices slices) => Apply(slices.TurnState);
}
