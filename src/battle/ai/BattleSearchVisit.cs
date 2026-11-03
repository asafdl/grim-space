using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Dfs;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Ai;

internal readonly record struct CapabilitySearchState(
	Coord Position,
	Coord Fore,
	Coord Dorsal,
	Coord Starboard,
	string MountFingerprint);

internal readonly record struct MoveSearchState(
	Coord Position,
	Coord Fore,
	Coord Dorsal,
	Coord Starboard);

internal readonly record struct MovePreviewSearchState(
	Coord Position,
	Coord Fore,
	Coord Dorsal,
	Coord Starboard,
	int CurrentHullPoints,
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
		return new SearchVisitState(
			new CapabilitySearchState(
				actor.Position,
				actor.Fore,
				actor.Dorsal,
				actor.Starboard,
				MountFingerprint(actor)),
			[
				actor.CurrentActionPoints,
				actor.CurrentManeuverPoints,
				-sim.Actions.Count(action => action.ActorId == actorId),
			]);
	}

	public static SearchVisitState ForMove(BattleSimulation sim, string actorId)
	{
		var actor = sim.StateOf<ActorState>(actorId);
		return new SearchVisitState(
			new MoveSearchState(
				actor.Position,
				actor.Fore,
				actor.Dorsal,
				actor.Starboard),
			[
				actor.CurrentActionPoints,
				actor.CurrentManeuverPoints,
				-sim.Actions.Count(action => action.ActorId == actorId),
			]);
	}

	public static SearchVisitState ForMovePreview(BattleSimulation sim, string actorId)
	{
		var actor = sim.StateOf<ActorState>(actorId);
		return new SearchVisitState(
			new MovePreviewSearchState(
				actor.Position,
				actor.Fore,
				actor.Dorsal,
				actor.Starboard,
				actor.CurrentHullPoints,
				actor.CurrentShieldPoints[ESpatialOrientation.Forward],
				actor.CurrentShieldPoints[ESpatialOrientation.Retro],
				actor.CurrentShieldPoints[ESpatialOrientation.Port],
				actor.CurrentShieldPoints[ESpatialOrientation.Starboard],
				actor.CurrentShieldPoints[ESpatialOrientation.Dorsal],
				actor.CurrentShieldPoints[ESpatialOrientation.Ventral]),
			[
				actor.CurrentActionPoints,
				actor.CurrentManeuverPoints,
				-sim.Actions.Count(action => action.ActorId == actorId),
			]);
	}

	private static string MountFingerprint(ActorState actor) =>
		string.Join(
			'|',
			actor.Loadout.InstalledAbilities.Select(installed =>
			{
				var runtime = actor.MountRuntimeFor(installed.Mount);
				return $"{installed.Spec.Kind}:{installed.MountedOn}:{runtime.UsesRemaining}:{runtime.CooldownRemaining}";
			}));
}
