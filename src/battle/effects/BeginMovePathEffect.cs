using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class BeginMovePathEffect : IEffect<BattleWorld, ActorRuntime>
{
	public void Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		runtime.MinPathApCost = ActorRuntime.InitialMinPathApCost;
		runtime.PathApSpent = 0;
		runtime.PathForwardSteps = 0;
		runtime.UsedDirectionsMask = 0;
		runtime.MoveStartMomentumLevel = world.StateOf(actorId).MomentumLevel;
		runtime.MovementBuildupLevel = world.StateOf(actorId).MomentumLevel;
		runtime.MovementBuildupForwardSteps = 0;
	}
}
