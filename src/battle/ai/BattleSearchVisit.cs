using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Ai;

internal readonly record struct CapabilitySearchState(
	Coord Position,
	Coord Fore,
	Coord Dorsal,
	Coord Starboard,
	int UsedDirectionsMask,
	int MomentumLevel,
	int MinPathApCost,
	int PathForwardSteps,
	int PathApSpent,
	bool SpinBraked,
	bool SpinDiscount,
	int ActionPoints,
	int FlakRemaining,
	int RailgunRemaining);

internal static class BattleSearchVisit
{
	public static SearchVisitState ForCapabilities(BattleSimulation sim, string actorId)
	{
		var actor = sim.StateOf<ActorState>(actorId);
		var runtime = sim.RuntimeFor(actorId);
		var path = runtime.ActivePath;
		return new SearchVisitState(
			new CapabilitySearchState(
				actor.Position,
				actor.Fore,
				actor.Dorsal,
				actor.Starboard,
				path?.UsedDirectionsMask ?? 0,
				actor.MomentumLevel,
				path?.MinPathApCost ?? MovePathSession.InitialMinPathApCost,
				path?.PathForwardSteps ?? 0,
				path?.PathApSpent ?? 0,
				path?.SpinBraked ?? false,
				runtime.SpinDiscount,
				actor.ActionPoints,
				actor.FlakRemaining,
				actor.RailgunRemaining),
			[]);
	}
}
