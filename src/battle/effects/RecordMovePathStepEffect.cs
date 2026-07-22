using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Turn;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Battle.Effects;

public sealed class RecordMovePathStepEffect(EStepDirection direction, int directionBit) : IEffect<BattleSlices>
{
	public void Apply(TurnPhaseContext phaseContext)
	{
		phaseContext.UsedDirectionsMask |= directionBit;
		if (direction == EStepDirection.Forward)
			phaseContext.PathForwardSteps++;
	}

	void IEffect<BattleSlices>.Apply(BattleSlices slices) => Apply(slices.PhaseContext);
}
