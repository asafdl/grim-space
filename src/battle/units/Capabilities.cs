using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Abilities;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;
using GrimSpace.Battle.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Units;

public static class Capabilities
{
	internal const string PreviewPatrolId = "__preview_patrol__";
	internal const string PreviewTorpedoId = "__preview_torpedo__";

	public static IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> Movement { get; } =
	[
		MoveDef.Instance,
		HeadingDef.Instance,
		RollDef.Instance,
	];

	/// <summary>
	/// Chassis-default ability defs for HUD metadata and tests. Combat discovery uses <see cref="For"/> with actor state.
	/// </summary>
	public static IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> AbilitiesFor(
		EType type) =>
		type == EType.Torpedo
			? [DetonateDef.Instance]
			: AbilityDefsForLoadout(ChassisSpec(type).NewDefaultLoadout().InstalledAbilities);

	public static IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> AbilityDefsForLoadout(
		IReadOnlyList<InstalledAbility> installed) =>
		CollectAbilityDefs(installed);

	public static IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> For(
		State state)
	{
		if (state.Type == EType.Torpedo)
			return [TorpedoMoveDef.Instance, DetonateDef.Instance];

		return [..Movement, ..AbilityDefsForLoadout(state.Loadout.InstalledAbilities)];
	}

	/// <summary>
	/// Chassis-default movement + abilities for previews that lack a live <see cref="State"/>.
	/// </summary>
	public static IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> For(
		EType type) =>
		type switch
		{
			EType.Torpedo => [TorpedoMoveDef.Instance, ..AbilitiesFor(EType.Torpedo)],
			_ => [..Movement, ..AbilitiesFor(type)],
		};

	public static IReadOnlyList<IAction> LegalCapabilities(BattleSimulation sim, string actorId)
	{
		var world = sim.World;
		var runtime = sim.RuntimeFor(actorId);
		var state = world.StateOf(actorId);
		var legal = new List<IAction>();

		foreach (var def in AbilityDefsFor(state))
		{
			var candidates = def switch
			{
				TorpedoDef torpedo => torpedo.Discover(actorId, PreviewTorpedoId, world),
				SpawnPatrolDef => DiscoverPreviewPatrolSpawns(state),
				_ => def.Discover(world, runtime, actorId),
			};

			foreach (var action in candidates)
			{
				if (sim.Peek(action) is not null)
					legal.Add(action);
			}
		}

		return legal;
	}

	public static bool IsLegalCapability(
		IReadOnlyList<IAction> legalCapabilities,
		IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> def,
		ESpatialOrientation? mountedOn) =>
		legalCapabilities.Any(action =>
			action is IAction<BattleWorld, ActorRuntime> typed
			&& ReferenceEquals(typed.Definition, def)
			&& (action is not IMountedAction mounted
				|| mountedOn == mounted.MountedOn));

	internal static IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> DefForKind(
		EAbilityKind kind) =>
		kind switch
		{
			EAbilityKind.Flak => FlakDef.Instance,
			EAbilityKind.Railgun => RailgunDef.Instance,
			EAbilityKind.PatrolBay => SpawnPatrolDef.Instance,
			EAbilityKind.TorpedoLauncher => TorpedoDef.Instance,
			_ => throw new InvalidOperationException($"No action definition for ability kind '{kind}'."),
		};

	private static IEnumerable<IAction> DiscoverPreviewPatrolSpawns(State state)
	{
		foreach (var installed in state.Loadout.InstalledAbilities)
		{
			if (installed.Spec.Kind == EAbilityKind.PatrolBay)
				yield return new SpawnPatrolAction(state.Id, installed.MountedOn, PreviewPatrolId);
		}
	}

	private static IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> AbilityDefsFor(
		State state)
	{
		if (state.Type == EType.Torpedo)
			return [DetonateDef.Instance];

		return AbilityDefsForLoadout(state.Loadout.InstalledAbilities);
	}

	private static IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> CollectAbilityDefs(
		IReadOnlyList<InstalledAbility> installed)
	{
		var defs = new List<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>>();
		var seen = new HashSet<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>>();
		foreach (var ability in installed)
		{
			var def = DefForKind(ability.Spec.Kind);
			if (seen.Add(def))
				defs.Add(def);
		}

		return defs;
	}

	private static ShipSpec ChassisSpec(EType type) =>
		type switch
		{
			EType.Fighter => FighterSpec.Instance,
			EType.Carrier => CarrierSpec.Instance,
			EType.Patrol => PatrolSpec.Instance,
			EType.Torpedo => TorpedoSpec.Instance,
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
		};
}
