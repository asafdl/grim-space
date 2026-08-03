using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public sealed class MovePathStepEffect(
	MoveStepAction step,
	Coord destination,
	int stepApCost,
	int directionBit) : IEffect<BattleWorld, ActorRuntime>
{
	public void Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		if (runtime.ActivePath is null)
		{
			runtime.ActivePath = MovePathSession.Begin(
				actorId,
				actor.Position,
				BodyFrame.From(actor),
				actor.MomentumLevel);
		}

		runtime.ActivePath.ApplyStep(step, destination, stepApCost, directionBit);

		if (step.Direction == ESpatialOrientation.Retro)
		{
			runtime.ActivePath.MarkSpinBraked();
			runtime.SpinBraked = true;
			runtime.SpinDiscount = true;
		}
	}
}
