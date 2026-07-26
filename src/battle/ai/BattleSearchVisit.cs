using GrimSpace.Battle.Actions;
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

internal readonly record struct BattleSearchState(
	Coord Position,
	Coord Fore,
	Coord Dorsal,
	int MomentumLevel,
	int UsedDirectionsMask,
	int PathForwardSteps,
	int MinPathApCost,
	int PathApSpent,
	int RawYawQuarters,
	int MoveStartMomentumLevel,
	int MovementBuildupLevel,
	int MovementBuildupForwardSteps,
	int MomentumPaid,
	int MomentumGainedFromMovement,
	bool SpinBraked,
	bool SpinDiscount,
	int HazardSignature);

internal static class BattleSearchVisit
{
	public static SearchVisitState MoveVisit(BattleSimulation sim, string actorId) =>
		ForMove(sim, actorId);

	public static SearchVisitState TurnVisit(BattleSimulation sim, string actorId) =>
		ForTurn(sim, actorId);

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

	public static SearchVisitState ForTurn(BattleSimulation sim, string actorId)
	{
		var actor = sim.StateOf<ActorState>(actorId);
		var runtime = sim.RuntimeFor(actorId);
		return new SearchVisitState(
			new BattleSearchState(
				actor.Position,
				actor.Fore,
				actor.Dorsal,
				actor.MomentumLevel,
				runtime.UsedDirectionsMask,
				runtime.PathForwardSteps,
				runtime.MinPathApCost,
				runtime.PathApSpent,
				runtime.RawYawQuarters,
				runtime.MoveStartMomentumLevel,
				runtime.MovementBuildupLevel,
				runtime.MovementBuildupForwardSteps,
				runtime.MomentumPaid,
				runtime.MomentumGainedFromMovement,
				runtime.SpinBraked,
				runtime.SpinDiscount,
				HashHazards(sim)),
			[
				actor.ActionPoints,
				actor.MissilesRemaining,
				actor.FlakRemaining,
				actor.RailgunRemaining,
			]);
	}

	private static int HashHazards(BattleSimulation sim)
	{
		var hash = 0;
		for (var tick = sim.AnchorTick + 1; tick <= sim.TimelineMaxTick; tick++)
		{
			foreach (var action in sim.PeekTimeline(tick))
			{
				if (action is not ResolveHazardAction hazard)
					continue;

				hash = HashCode.Combine(hash, tick);
				foreach (var cell in hazard.Cells)
					hash = HashCode.Combine(hash, cell);
			}
		}

		return hash;
	}
}
