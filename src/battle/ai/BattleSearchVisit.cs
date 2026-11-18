using System.Globalization;
using System.Text;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Dfs;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Ai;

// Heat is part of the key, not the budget: a cooler state with more AP/MP cannot always reproduce a
// hotter state's Overdrive shot, and higher heat is not uniformly better.
// TODO: exact heat multiplies frames (~9.6x on DevGooper). Replace with a shortcut heuristic, e.g. key exact
// heat only while Overdrive is still reachable this turn, otherwise collapse it to zero / non-zero.
internal readonly record struct CapabilitySearchState(
	Coord Position,
	Coord Fore,
	Coord Dorsal,
	Coord Starboard,
	string MountFingerprint,
	string DamageFingerprint,
	int Heat,
	bool Overdrive,
	bool Overheat);

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
	private static readonly ESpatialOrientation[] Faces = Enum.GetValues<ESpatialOrientation>();

	public static SearchVisitState ForCapabilities(BattleSimulation sim, string actorId)
	{
		var actor = sim.StateOf<ActorState>(actorId);
		return new SearchVisitState(
			new CapabilitySearchState(
				actor.Position,
				actor.Fore,
				actor.Dorsal,
				actor.Starboard,
				MountFingerprint(actor),
				DamageFingerprint(sim),
				actor.Heat,
				actor.Overdrive,
				actor.Overheat),
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

	private static string DamageFingerprint(BattleSimulation sim)
	{
		var fingerprint = new StringBuilder();
		foreach (var unit in sim.World.UnitRegistry.All.OrderBy(unit => unit.State.Id, StringComparer.Ordinal))
		{
			var state = unit.State;
			fingerprint.Append(CultureInfo.InvariantCulture,
				$"{state.Id.Length}:{state.Id}:{state.CurrentHullPoints}");
			foreach (var face in Faces)
				fingerprint.Append(CultureInfo.InvariantCulture, $":{state.CurrentShieldPoints[face]}");
			fingerprint.Append('|');
		}
		return fingerprint.ToString();
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
