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
	internal const string PreviewRepurposedMinerId = "__preview_repurposed_miner__";
	internal const string PreviewVoidBombId = "__preview_void_bomb__";
	internal const string PreviewGoopHazardId = "__preview_goop_hazard__";

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
		type == EType.VoidBomb
			? [DetonateDef.Instance]
			: AbilityDefsForLoadout(ChassisSpec(type).NewDefaultLoadout().InstalledAbilities);

	public static IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> AbilityDefsForLoadout(
		IReadOnlyList<InstalledAbility> installed) =>
		CollectAbilityDefs(installed);

	public static IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> For(
		State state)
	{
		if (state.Type == EType.VoidBomb)
			return [VoidBombMoveDef.Instance, DetonateDef.Instance];

		return [..Movement, ..AbilityDefsForLoadout(state.Loadout.InstalledAbilities)];
	}

	/// <summary>
	/// Chassis-default movement + abilities for previews that lack a live <see cref="State"/>.
	/// </summary>
	public static IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> For(
		EType type) =>
		type switch
		{
			EType.VoidBomb => [VoidBombMoveDef.Instance, ..AbilitiesFor(EType.VoidBomb)],
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
				VoidBombDef torpedo => torpedo.Discover(actorId, PreviewVoidBombId, world),
				GoopGunDef goopGun => goopGun.Discover(actorId, PreviewGoopHazardId, world),
				SpawnRepurposedMinerDef => DiscoverPreviewRepurposedMinerSpawns(state),
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
			EAbilityKind.ScrapDroneSwarm => ScrapDroneSwarmDef.Instance,
			EAbilityKind.LightningCannon => LightningCannonDef.Instance,
			EAbilityKind.MinerBay => SpawnRepurposedMinerDef.Instance,
			EAbilityKind.VoidBombLauncher => VoidBombDef.Instance,
			EAbilityKind.GoopGun => GoopGunDef.Instance,
			_ => throw new InvalidOperationException($"No action definition for ability kind '{kind}'."),
		};

	private static IEnumerable<IAction> DiscoverPreviewRepurposedMinerSpawns(State state)
	{
		foreach (var installed in state.Loadout.InstalledAbilities)
		{
			if (installed.Spec.Kind == EAbilityKind.MinerBay)
				yield return new SpawnRepurposedMinerAction(state.Id, installed.MountedOn, PreviewRepurposedMinerId);
		}
	}

	private static IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> AbilityDefsFor(
		State state)
	{
		if (state.Type == EType.VoidBomb)
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
			EType.Gunship => GunshipSpec.Instance,
			EType.Carrier => CarrierSpec.Instance,
			EType.RepurposedMiner => RepurposedMinerSpec.Instance,
			EType.VoidBomb => VoidBombSpec.Instance,
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
		};
}
