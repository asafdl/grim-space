using Godot;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Presentation.Ui;

public static class AbilityHudCatalog
{
	// Action bar + targeting preview accents (icons are white SVG, tinted at load).
	private static readonly Color ScrapDroneAccent = new(0.98f, 0.82f, 0.14f, 0.48f);
	private static readonly Color LightningAccent = new(0.38f, 0.68f, 1f, 0.48f);
	private static readonly Color VoidBombAccent = new(0.68f, 0.32f, 0.98f, 0.52f);
	private static readonly Color GoopGunAccent = new(0.42f, 0.88f, 0.38f, 0.48f);

	public sealed record Spec(
		EPlayerMode Mode,
		IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Def,
		AbilityTargetingSpec Targeting,
		string? IconPath,
		Func<UnitDisplayState, string> Tooltip,
		Func<UnitDisplayState, AbilityLegality, string> Charges,
		Func<AbilityLegality, bool> IsLegal)
	{
		public Color IconTint { get; } = OpaqueTint(Targeting.Tint);
	}

	private static Color OpaqueTint(Color previewTint) =>
		new(previewTint.R, previewTint.G, previewTint.B, 1f);

	/// <summary>Chassis-default HUD rows (tests and type-only previews).</summary>
	public static IReadOnlyList<Spec> ForUnit(EType type) =>
		ForActor(State.FromShipInstance(ShipInstance.FromCatalog("__hud__", type), Coord.Zero));

	public static IReadOnlyList<Spec> ForActor(State state) =>
		state.Type == EType.VoidBomb
			? [Resolve(DetonateDef.Instance)]
			: Capabilities.AbilityDefsForLoadout(state.Loadout.InstalledAbilities)
				.Select(Resolve)
				.ToList();

	public static IReadOnlyList<Spec> ForDisplayState(UnitDisplayState unit) =>
		ForActor(unit.ToState());

	public static AbilityBarSlotState BuildState(Spec spec, UnitDisplayState unit, AbilityLegality legality) =>
		new(spec.Tooltip(unit), spec.Charges(unit, legality), spec.IsLegal(legality));

	private static Spec Resolve(
		IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> def) =>
		def switch
		{
			ScrapDroneSwarmDef => new(
				EPlayerMode.ScrapDroneSwarm,
				def,
				new AbilityTargetingSpec(
					AdjacentMountedSource,
					AbilitySourceMeshes.CreateScrapDroneSwarmBurst,
					ScrapDroneAccent),
				"res://assets/ui/abilities/scrap_drone_swarm.svg",
				BattleHudCopy.ScrapDroneSwarmTooltipFor,
				(unit, _) => BattleHudCopy.Charges(
					unit.UsesRemaining(EAbilityKind.ScrapDroneSwarm),
					unit.MaxUsesPerTurn(EAbilityKind.ScrapDroneSwarm)),
				legality => legality.Weapons.IsKindLegal(EWeaponKind.ScrapDroneSwarm)),
			LightningCannonDef => new(
				EPlayerMode.LightningCannon,
				def,
				new AbilityTargetingSpec(
					AdjacentMountedSource,
					AbilitySourceMeshes.CreateLightningCannon,
					LightningAccent),
				"res://assets/ui/abilities/lightning_cannon.svg",
				BattleHudCopy.LightningCannonTooltipFor,
				(unit, _) => BattleHudCopy.Charges(
					unit.UsesRemaining(EAbilityKind.LightningCannon),
					unit.MaxUsesPerTurn(EAbilityKind.LightningCannon)),
				legality => legality.Weapons.IsKindLegal(EWeaponKind.LightningCannon)),
			GoopGunDef => new(
				EPlayerMode.GoopGun,
				def,
				new AbilityTargetingSpec(
					ForwardSource<GoopGunAction>,
					AbilitySourceMeshes.CreateGoopGun,
					GoopGunAccent),
				"res://assets/ui/abilities/goop_gun.svg",
				BattleHudCopy.GoopGunTooltipFor,
				(unit, _) => BattleHudCopy.Charges(
					unit.ReadyMounts(EAbilityKind.GoopGun),
					unit.MountCount(EAbilityKind.GoopGun)),
				legality => legality.Weapons.IsKindLegal(EWeaponKind.GoopGun)),
			VoidBombDef => new(
				EPlayerMode.VoidBomb,
				def,
				new AbilityTargetingSpec(
					VoidBombSource,
					AbilitySourceMeshes.CreateVoidBomb,
					VoidBombAccent),
				"res://assets/ui/abilities/void_bomb.svg",
				BattleHudCopy.VoidBombTooltipFor,
				(unit, _) => BattleHudCopy.Charges(
					unit.ReadyMounts(EAbilityKind.VoidBombLauncher),
					unit.MountCount(EAbilityKind.VoidBombLauncher)),
				legality => legality.Weapons.IsKindLegal(EWeaponKind.VoidBomb)),
			DetonateDef => new(
				EPlayerMode.Detonate,
				def,
				new AbilityTargetingSpec(
					SelfSource<DetonateAction>,
					AbilitySourceMeshes.CreateDetonation,
					new Color(1f, 0.42f, 0.18f, 0.5f)),
				"res://assets/ui/abilities/detonate.svg",
				BattleHudCopy.DetonateTooltipFor,
				(unit, _) => BattleHudCopy.Charges(
					unit.FuelRemaining,
					unit.Projectile?.FuelTurns ?? 0),
				legality => legality.Detonate),
			SpawnRepurposedMinerDef => new(
				EPlayerMode.SpawnRepurposedMiner,
				def,
				new AbilityTargetingSpec(
					RepurposedMinerSource,
					AbilitySourceMeshes.CreateRepurposedMiner,
					new Color(0.4f, 0.9f, 0.58f, 0.48f)),
				"res://assets/ui/abilities/repurposed-miner.svg",
				BattleHudCopy.SpawnRepurposedMinerTooltipFor,
				(unit, _) => BattleHudCopy.Charges(
					unit.ReadyMounts(EAbilityKind.MinerBay),
					unit.MountCount(EAbilityKind.MinerBay)),
				legality => legality.SpawnRepurposedMiner),
			_ => throw new NotSupportedException(
				$"No ability HUD metadata is registered for {def.GetType().Name}."),
		};

	private static AbilitySourcePose AdjacentMountedSource(State actor, IAction action)
	{
		if (action is not IMountedAction mounted)
			throw new ArgumentException($"Expected {nameof(IMountedAction)}.", nameof(action));

		var frame = BodyFrame.From(actor);
		var direction = frame.Step(mounted.MountedOn);
		return new AbilitySourcePose(
			actor.Position + direction,
			direction,
			actor.Dorsal,
			mounted.MountedOn);
	}

	private static AbilitySourcePose ForwardSource<TAction>(State actor, IAction action)
		where TAction : IAction
	{
		Require<TAction>(action);
		return new AbilitySourcePose(actor.Position + actor.Fore, actor.Fore, actor.Dorsal);
	}

	private static AbilitySourcePose VoidBombSource(State actor, IAction action)
	{
		var voidBomb = Require<VoidBombAction>(action);
		var pose = VoidBombMount.LaunchPose(actor, voidBomb.MountedOn);
		return new AbilitySourcePose(
			pose.Position,
			pose.Fore,
			pose.Dorsal,
			voidBomb.MountedOn);
	}

	private static AbilitySourcePose RepurposedMinerSource(State actor, IAction action)
	{
		var repurposedMiner = Require<SpawnRepurposedMinerAction>(action);
		var pose = MinerBayMount.LaunchPose(actor, repurposedMiner.MountedOn);
		return new AbilitySourcePose(
			pose.Position,
			pose.Fore,
			pose.Dorsal,
			repurposedMiner.MountedOn);
	}

	private static AbilitySourcePose SelfSource<TAction>(State actor, IAction action)
		where TAction : IAction
	{
		Require<TAction>(action);
		return new AbilitySourcePose(actor.Position, actor.Fore, actor.Dorsal);
	}

	private static TAction Require<TAction>(IAction action)
		where TAction : IAction =>
		action is TAction typed
			? typed
			: throw new ArgumentException(
				$"Expected {typeof(TAction).Name}.",
				nameof(action));
}
