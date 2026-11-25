using System.Globalization;
using System.Text;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Dfs;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
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

internal sealed record CapabilitySearchWorldState(
	string World,
	IReadOnlyList<object?> Rules)
{
	public bool Equals(CapabilitySearchWorldState? other) =>
		other is not null && World == other.World && Rules.SequenceEqual(other.Rules);

	public override int GetHashCode()
	{
		var hash = new HashCode();
		hash.Add(World, StringComparer.Ordinal);
		foreach (var rule in Rules)
			hash.Add(rule);
		return hash.ToHashCode();
	}
}

internal static class BattleSearchVisit
{
	private static readonly ESpatialOrientation[] Faces = Enum.GetValues<ESpatialOrientation>();

	public static Func<BattleSimulation, string, SearchVisitState> ForCapabilitySearch(BattleSimulation anchor)
	{
		var start = anchor.Actions.Count;
		var originalIds = anchor.World.UnitRegistry.Ids.ToHashSet(StringComparer.Ordinal);
		var terrain = anchor.World.NonUnits.ToDictionary();
		return (simulation, actorId) =>
		{
			var aliases = new Dictionary<string, string>(StringComparer.Ordinal);
			foreach (var action in simulation.Actions.Skip(start).OfType<VoidBombAction>())
			{
				if (!originalIds.Contains(action.SpawnedUnitId)
					&& simulation.World.UnitRegistry.Contains(action.SpawnedUnitId))
					aliases.TryAdd(action.SpawnedUnitId, $"spawn:{aliases.Count}");
			}
			string Identity(string id) =>
				aliases.TryGetValue(id, out var alias) ? alias : $"original:{id}";

			var actor = simulation.StateOf<ActorState>(actorId);
			var fingerprint = new StringBuilder();
			var rules = new List<object?>();
			foreach (var unit in simulation.World.UnitRegistry.All)
				AppendUnit(fingerprint, rules, unit, actorId, Identity);
			AppendTerrainChanges(fingerprint, simulation.World, terrain, Identity);
			return new SearchVisitState(
				new CapabilitySearchWorldState(fingerprint.ToString(), rules),
				[
					actor.CurrentActionPoints,
					actor.CurrentManeuverPoints,
					-simulation.Actions.Count(action => action.ActorId == actorId),
				]);
		};
	}

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

	private static void AppendText(StringBuilder fingerprint, string value) =>
		fingerprint.Append(CultureInfo.InvariantCulture, $"{value.Length}:").Append(value);

	private static void AppendUnit(
		StringBuilder fingerprint, List<object?> rules, Unit unit, string actorId, Func<string, string> identity)
	{
		var state = unit.State;
		AppendText(fingerprint, identity(state.Id));
		AppendText(fingerprint, identity(state.ParentId));
		fingerprint.Append(CultureInfo.InvariantCulture,
			$":{unit.Team}:{unit.Faction}:{state.Type}:{state.Initiative}:{state.Position}:{state.Fore}:{state.Dorsal}:{state.Starboard}"
			+ $":{state.CurrentHullPoints}:{state.FuelRemaining}:{state.Projectile}"
			+ $":{state.Heat}:{state.Overdrive}:{state.Overheat}"
			+ $":{state.ShieldsUpUsedThisTurn}:{state.HeatVentsUsedThisTurn}:{state.ShieldBraceUsedThisTurn}"
			+ $":{state.OverchargeEnginesCooldownRemaining}:{state.MarkWeakspotCooldownRemaining}"
			+ $":{state.DefensiveManeuversMpUsed}:{state.ShieldDynamoGrantedPerFace}");
		if (state.Id != actorId)
			fingerprint.Append(CultureInfo.InvariantCulture, $":{state.CurrentActionPoints}:{state.CurrentManeuverPoints}");
		AppendText(fingerprint, MountFingerprint(state));
		foreach (var face in Faces)
			fingerprint.Append(CultureInfo.InvariantCulture,
				$":{state.CurrentShieldPoints[face]}:{state.Loadout.BaseShieldPoints[face]}");
		fingerprint.Append(CultureInfo.InvariantCulture, $":{state.Loadout.BaseHullPoints}");
		foreach (var plume in state.CryoPlumesCooledThisTurn.Order(StringComparer.Ordinal))
			AppendText(fingerprint, plume);
		fingerprint.Append(';');
		_ = rules;
		foreach (var status in state.Statuses)
			AppendStatus(fingerprint, status);
		fingerprint.Append(']');
	}

	private static void AppendStatus(StringBuilder fingerprint, BattleStatus status)
	{
		AppendText(fingerprint, status is TemporaryShieldGrantStatus
			? nameof(TemporaryShieldGrantStatus)
			: status.GetType().Name);
		fingerprint.Append(CultureInfo.InvariantCulture, $":{status.Polarity}");
		switch (status)
		{
			case TemporaryShieldGrantStatus temporary:
				foreach (var face in Faces)
					fingerprint.Append(CultureInfo.InvariantCulture, $":{temporary.Remaining[face]}");
				break;
			case ShieldBraceStatus brace:
				fingerprint.Append(CultureInfo.InvariantCulture, $":{brace.Face}:{brace.DamagePending}");
				foreach (var face in Faces)
					fingerprint.Append(CultureInfo.InvariantCulture, $":{brace.Snapshot[face]}");
				break;
			case MarkWeakspotDebuff mark:
				fingerprint.Append(CultureInfo.InvariantCulture, $":{mark.Face}");
				break;
			case ThirstForScrap thirst:
				fingerprint.Append(CultureInfo.InvariantCulture, $":{thirst.Stacks}");
				break;
			case ReactiveShields reactive:
				fingerprint.Append(CultureInfo.InvariantCulture, $":{reactive.Stacks}");
				break;
		}
		fingerprint.Append('|');
	}

	private static void AppendTerrainChanges(
		StringBuilder fingerprint, BattleWorld world, IReadOnlyDictionary<string, NonUnit> terrain,
		Func<string, string> identity)
	{
		foreach (var nonUnit in world.NonUnits.Values)
		{
			if (terrain.TryGetValue(nonUnit.Id, out var original) && ReferenceEquals(nonUnit, original))
				continue;
			AppendText(fingerprint, nonUnit.Id);
			AppendText(fingerprint, identity(nonUnit.ActorId));
			AppendText(fingerprint, nonUnit.GetType().FullName!);
			fingerprint.Append(nonUnit.Frame);
			foreach (var cell in nonUnit.Cells.OrderBy(cell => cell.X).ThenBy(cell => cell.Y).ThenBy(cell => cell.Z))
				fingerprint.Append(cell);
			if (nonUnit is AsteroidBelt belt)
				fingerprint.Append(CultureInfo.InvariantCulture, $":{belt.DamageResistancePercent}");
			fingerprint.Append('|');
		}
		foreach (var id in terrain.Keys)
		{
			if (!world.NonUnits.ContainsKey(id))
				AppendText(fingerprint, $"removed:{id}");
		}
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
