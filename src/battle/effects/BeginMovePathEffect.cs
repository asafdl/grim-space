using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class BeginMovePathEffect : IEffect<BattleBoard, ActorSession>
{
	public void Apply(BattleBoard world, ActorSession runtime, string actorId)
	{
		runtime.MinPathApCost = ActorSession.InitialMinPathApCost;
		runtime.PathApSpent = 0;
		runtime.PathForwardSteps = 0;
		runtime.UsedDirectionsMask = 0;
		runtime.MoveStartMomentumLevel = world.StateOf(actorId).MomentumLevel;
		runtime.MovementBuildupLevel = world.StateOf(actorId).MomentumLevel;
		runtime.MovementBuildupForwardSteps = 0;
	}
}
