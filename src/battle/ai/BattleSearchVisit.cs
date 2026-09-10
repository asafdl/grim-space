using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Dfs;
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

internal readonly record struct MoveSearchState(
	Coord Position,
	Coord Fore,
	Coord Dorsal,
	Coord Starboard,
	int UsedDirectionsMask,
	int MomentumLevel,
	int MinPathApRemaining,
	int PathForwardSteps,
	int PathApSpent,
	bool SpinBraked,
	int ActionPoints);

internal readonly record struct MovePreviewSearchState(
	Coord Position,
	Coord Fore,
	Coord Dorsal,
	Coord Starboard,
	int HullPoints,
	int ForwardShield,
	int RetroShield,
	int PortShield,
	int StarboardShield,
	int DorsalShield,
	int VentralShield);

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
				path?.MinPathApRemaining ?? actor.Stats.MinPathApCost,
				path?.PathForwardSteps ?? 0,
				path?.PathApSpent ?? 0,
				path?.SpinBraked ?? false,
				runtime.SpinDiscount,
				actor.ActionPoints,
				actor.FlakRemaining,
				actor.RailgunRemaining),
			[]);
	}

	public static SearchVisitState ForMove(BattleSimulation sim, string actorId)
	{
		var actor = sim.StateOf<ActorState>(actorId);
		var runtime = sim.RuntimeFor(actorId);
		var path = runtime.ActivePath;
		return new SearchVisitState(
			new MoveSearchState(
				actor.Position,
				actor.Fore,
				actor.Dorsal,
				actor.Starboard,
				path?.UsedDirectionsMask ?? 0,
				actor.MomentumLevel,
				path?.MinPathApRemaining ?? actor.Stats.MinPathApCost,
				path?.PathForwardSteps ?? 0,
				path?.PathApSpent ?? 0,
				path?.SpinBraked ?? false,
				actor.ActionPoints),
			[]);
	}

	public static SearchVisitState ForMovePreview(BattleSimulation sim, string actorId)
	{
		var actor = sim.StateOf<ActorState>(actorId);
		var moves = sim.Actions.OfType<MoveStepAction>().Where(action => action.ActorId == actorId).ToList();
		return new SearchVisitState(
			new MovePreviewSearchState(
				actor.Position,
				actor.Fore,
				actor.Dorsal,
				actor.Starboard,
				actor.HullPoints,
				actor.ShieldPoints[ESpatialOrientation.Forward],
				actor.ShieldPoints[ESpatialOrientation.Retro],
				actor.ShieldPoints[ESpatialOrientation.Port],
				actor.ShieldPoints[ESpatialOrientation.Starboard],
				actor.ShieldPoints[ESpatialOrientation.Dorsal],
				actor.ShieldPoints[ESpatialOrientation.Ventral]),
			[
				actor.ActionPoints,
				-moves.Count(action => action.Heading is not null),
				-moves.Count(action => action.Roll is not null),
			]);
	}
}
