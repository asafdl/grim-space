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
	ManeuverProgress ManeuverProgress,
	int ActionPoints,
	int FlakRemaining,
	int RailgunRemaining);

internal readonly record struct MoveSearchState(
	Coord Position,
	Coord Fore,
	Coord Dorsal,
	Coord Starboard,
	int ActionPoints,
	ManeuverProgress ManeuverProgress);

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
	int VentralShield,
	ManeuverProgress ManeuverProgress);

internal static class BattleSearchVisit
{
	public static SearchVisitState ForCapabilities(BattleSimulation sim, string actorId)
	{
		var actor = sim.StateOf<ActorState>(actorId);
		return new SearchVisitState(
			new CapabilitySearchState(
				actor.Position,
				actor.Fore,
				actor.Dorsal,
				actor.Starboard,
				ManeuverInvariant.ProgressOf(sim.Actions, actorId),
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
				actor.ActionPoints,
				ManeuverInvariant.ProgressOf(sim.Actions, actorId)),
			[]);
	}

	public static SearchVisitState ForMovePreview(BattleSimulation sim, string actorId)
	{
		var actor = sim.StateOf<ActorState>(actorId);
		var headings = sim.Actions.Count(action => action is HeadingTurnAction && action.ActorId == actorId);
		var rolls = sim.Actions.Count(action => action is RollAction && action.ActorId == actorId);
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
				actor.ShieldPoints[ESpatialOrientation.Ventral],
				ManeuverInvariant.ProgressOf(sim.Actions, actorId)),
			[
				actor.ActionPoints,
				-headings,
				-rolls,
			]);
	}

}
