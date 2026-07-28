using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Ai;

internal readonly record struct MoveSearchState(
	Coord Position,
	int UsedDirectionsMask,
	int MomentumLevel,
	int MinPathApCost,
	int ActionPoints);

internal static class BattleSearchVisit
{
	public static SearchVisitState MoveVisit(BattleSimulation sim, string actorId) =>
		ForMove(sim, actorId);

	public static SearchVisitState ForMove(BattleSimulation sim, string actorId)
	{
		var actor = sim.StateOf<ActorState>(actorId);
		var runtime = sim.RuntimeFor(actorId);
		return new SearchVisitState(
			new MoveSearchState(
				actor.Position,
				runtime.UsedDirectionsMask,
				actor.MomentumLevel,
				runtime.MinPathApCost,
				actor.ActionPoints),
			[]);
	}
}
