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
	int MomentumLevel,
	bool SpinDiscount,
	int ActionPoints,
	int FlakRemaining,
	int RailgunRemaining);

internal readonly record struct MoveSearchState(
	Coord Position,
	Coord Fore,
	Coord Dorsal,
	Coord Starboard,
	int MomentumLevel,
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
		return new SearchVisitState(
			new CapabilitySearchState(
				actor.Position,
				actor.Fore,
				actor.Dorsal,
				actor.Starboard,
				actor.MomentumLevel,
				runtime.SpinDiscount,
				actor.ActionPoints,
				actor.FlakRemaining,
				actor.RailgunRemaining),
			[]);
	}

	public static SearchVisitState ForMove(BattleSimulation sim, string actorId)
	{
		var actor = sim.StateOf<ActorState>(actorId);
		return new SearchVisitState(
			new MoveSearchState(
				actor.Position,
				actor.Fore,
				actor.Dorsal,
				actor.Starboard,
				actor.MomentumLevel,
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
