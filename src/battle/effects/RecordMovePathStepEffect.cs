using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class RecordMovePathStepEffect(EStepDirection direction, int directionBit)
	: IEffect<BattleBoard, ActorSession>
{
	public void Apply(BattleBoard world, ActorSession runtime, string actorId)
	{
		runtime.UsedDirectionsMask |= directionBit;
		if (direction == EStepDirection.Forward)
			runtime.PathForwardSteps++;
	}
}
