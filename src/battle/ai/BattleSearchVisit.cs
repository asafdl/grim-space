using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Ai;

internal readonly record struct CapabilitySearchState(
	Coord Position,
	int UsedDirectionsMask,
	int MomentumLevel,
	int MinPathApCost,
	int ActionPoints,
	int MissilesRemaining,
	int FlakRemaining,
	int RailgunRemaining);

internal static class BattleSearchVisit
{
	public static SearchVisitState MoveVisit(BattleSimulation sim, string actorId) =>
		ForCapabilities(sim, actorId);

	public static SearchVisitState ForCapabilities(BattleSimulation sim, string actorId)
	{
		var actor = sim.StateOf<ActorState>(actorId);
		var runtime = sim.RuntimeFor(actorId);
		return new SearchVisitState(
			new CapabilitySearchState(
				actor.Position,
				runtime.UsedDirectionsMask,
				actor.MomentumLevel,
				runtime.MinPathApCost,
				actor.ActionPoints,
				actor.MissilesRemaining,
				actor.FlakRemaining,
				actor.RailgunRemaining),
			[]);
	}
}
