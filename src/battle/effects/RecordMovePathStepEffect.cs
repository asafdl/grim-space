using GrimSpace.Battle.World;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class RecordMovePathStepEffect(ESpatialOrientation direction, int directionBit)
	: IEffect<BattleWorld, ActorRuntime>
{
	public void Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		runtime.UsedDirectionsMask |= directionBit;
		if (direction == ESpatialOrientation.Forward)
			runtime.PathForwardSteps++;
	}
}
