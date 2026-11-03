using Godot;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Targeting;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Runtime;
using GrimSpace.Components;
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
	private static readonly Color BraceAccent = new(0.72f, 0.58f, 0.28f, 0.5f);
	private static readonly Color ProtectionDroneAccent = new(0.55f, 0.85f, 0.95f, 0.5f);

	public sealed record Visual(string IconPath, Color Tint);

	public static Visual VisualFor(EAbilityKind kind)
	{
		var icon = HudTooltipIconCatalog.Resolve(kind.ToString());
		return new Visual(icon.Path, icon.Color);
	}

	public sealed record Spec(
		EPlayerMode Mode,
		IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Def,
		AbilityTargetingSpec Targeting,
		string? IconPath,
		Func<UnitDisplayState, BattleHudTooltipContent> Tooltip,
		Func<UnitDisplayState, AbilityLegality, string> Charges,
		Func<AbilityLegality, bool> IsLegal,
		TargetPickSpec? TargetPick = null,
		bool RequiresFacePickAfterTargets = false)
	{
		public Color IconTint { get; } = OpaqueTint(Targeting.Tint);

		public ITargetedActionDef? TargetedDef =>
			TargetPick is null
				? null
				: Def as ITargetedActionDef
					?? throw new InvalidOperationException(
						$"{Def.GetType().Name} declares target picks but is not an {nameof(ITargetedActionDef)}.");
	}

	private static Color OpaqueTint(Color previewTint) =>
		new(previewTint.R, previewTint.G, previewTint.B, 1f);

	/// <summary>Chassis-default HUD rows (tests and type-only previews).</summary>
	public static IReadOnlyList<Spec> ForUnit(EType type) =>
		ForActor(
			State.FromShipInstance(ShipInstance.FromCatalog("__hud__", type), Coord.Zero),
			ActorRuntime.From(null));

	public static IReadOnlyList<Spec> ForActor(State state, ActorRuntime runtime) =>
		Capabilities.AbilityDefsFor(state, runtime).Select(Resolve).ToList();

	public static IReadOnlyList<Spec> ForDisplayState(UnitDisplayState unit) =>
		ForActor(
			unit.ToState(),
			ActorRuntime.From(new PilotBattleSnapshot(unit.Id, unit.PilotCapabilities)));

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
					AdjacentMountedSource,
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
			ShieldsUpDef => new(
				EPlayerMode.ShieldsUp,
				def,
				new AbilityTargetingSpec(
					SelfSource<ShieldsUpAction>,
					AbilitySourceMeshes.CreateDetonation,
					new Color(0.55f, 0.78f, 1f, 0.5f)),
				"res://assets/ui/abilities/shields_up.svg",
				BattleHudCopy.ShieldsUpTooltipFor,
				(unit, _) => BattleHudCopy.Charges(
					unit.ShieldsUpUsedThisTurn ? 0 : 1,
					1),
				legality => legality.ShieldsUp),
			OverchargeEnginesDef => new(
				EPlayerMode.OverchargeEngines,
				def,
				new AbilityTargetingSpec(
					SelfSource<OverchargeEnginesAction>,
					AbilitySourceMeshes.CreateDetonation,
					new Color(0.95f, 0.55f, 0.22f, 0.5f)),
				"res://assets/ui/abilities/yaw.svg",
				BattleHudCopy.OverchargeEnginesTooltipFor,
				(unit, _) => BattleHudCopy.Charges(
					unit.OverchargeEnginesCooldownRemaining == 0 ? 1 : 0,
					1),
				legality => legality.OverchargeEngines),
			HeatVentsDef => new(
				EPlayerMode.HeatVents,
				def,
				new AbilityTargetingSpec(
					SelfSource<HeatVentsAction>,
					AbilitySourceMeshes.CreateDetonation,
					new Color(0.78f, 0.3f, 0.08f, 0.5f)),
				"res://assets/ui/battle/heat.png",
				BattleHudCopy.HeatVentsTooltipFor,
				(unit, _) => BattleHudCopy.Charges(
					unit.HeatVentsUsedThisTurn ? 0 : 1,
					1),
				legality => legality.HeatVents),
			BraceDef => new(
				EPlayerMode.Brace,
				def,
				new AbilityTargetingSpec(
					AdjacentMountedSource,
					AbilitySourceMeshes.CreateDetonation,
					BraceAccent),
				"res://assets/ui/abilities/shields_up.svg",
				BattleHudCopy.BraceTooltipFor,
				(unit, _) => BattleHudCopy.Charges(
					unit.ShieldBraceUsedThisTurn || unit.ShieldBraceActive ? 0 : 1,
					1),
				legality => legality.Brace),
			ProtectionDroneDef => new(
				EPlayerMode.ProtectionDrone,
				def,
				new AbilityTargetingSpec(
					SelfSource<ProtectionDroneAction>,
					AbilitySourceMeshes.CreateScrapDroneSwarmBurst,
					ProtectionDroneAccent),
				"res://assets/ui/abilities/protection_drone.png",
				BattleHudCopy.ProtectionDroneTooltipFor,
				(unit, _) => BattleHudCopy.Charges(
					unit.ReadyMounts(EAbilityKind.ProtectionDrone),
					unit.MountCount(EAbilityKind.ProtectionDrone)),
				legality => legality.ProtectionDrone,
				ProtectionDroneDef.TargetPick),
			MarkWeakspotDef => new(
				EPlayerMode.MarkWeakspot,
				def,
				new AbilityTargetingSpec(
					SelfOrientedSource,
					AbilitySourceMeshes.CreateDetonation,
					new Color(0.95f, 0.35f, 0.45f, 0.5f)),
				"res://assets/ui/battle/crit.png",
				BattleHudCopy.MarkWeakspotTooltipFor,
				(unit, _) => BattleHudCopy.Charges(
					unit.MarkWeakspotCooldownRemaining == 0 ? 1 : 0,
					1),
				legality => legality.MarkWeakspot,
				MarkWeakspotDef.TargetPick,
				RequiresFacePickAfterTargets: true),
			RamDef => new(
				EPlayerMode.Ram,
				def,
				new AbilityTargetingSpec(
					AdjacentMountedSource,
					AbilitySourceMeshes.CreateDetonation,
					new Color(0.92f, 0.55f, 0.28f, 0.5f)),
				"res://assets/ui/abilities/ram.png",
				BattleHudCopy.RamTooltipFor,
				(unit, _) => BattleHudCopy.Charges(
					unit.UsesRemaining(EAbilityKind.Ram),
					unit.MaxUsesPerTurn(EAbilityKind.Ram)),
				legality => legality.Weapons.IsKindLegal(EWeaponKind.Ram)),
			HighSpeedRodDef => new(
				EPlayerMode.HighSpeedRod,
				def,
				new AbilityTargetingSpec(
					AdjacentMountedSource,
					AbilitySourceMeshes.CreateHighSpeedRod,
					new Color(0.2f, 0.95f, 0.9f, 0.5f)),
				"res://assets/ui/abilities/high_speed_rod.svg",
				BattleHudCopy.HighSpeedRodTooltipFor,
				(unit, _) => BattleHudCopy.Charges(
					unit.UsesRemaining(EAbilityKind.HighSpeedRod),
					unit.MaxUsesPerTurn(EAbilityKind.HighSpeedRod)),
				legality => legality.Weapons.IsKindLegal(EWeaponKind.HighSpeedRod)),
			RoadsWeDontNeedRoadsDef => new(
				EPlayerMode.RoadsWeDontNeedRoads,
				def,
				new AbilityTargetingSpec(
					JumpDestination,
					() => ShipMesh.CreateHull(EType.Rogue),
					new Color(0.65f, 0.45f, 1f, 0.5f)),
				"res://assets/ui/abilities/roads_we_dont_need_roads.svg",
				BattleHudCopy.RoadsWeDontNeedRoadsTooltipFor,
				(unit, _) => BattleHudCopy.Charges(
					unit.ReadyMounts(EAbilityKind.RoadsWeDontNeedRoads),
					unit.MountCount(EAbilityKind.RoadsWeDontNeedRoads)),
				legality => legality.RoadsWeDontNeedRoads),
			_ => throw new NotSupportedException(
				$"No ability HUD metadata is registered for {def.GetType().Name}."),
		};

	private static AbilitySourcePose AdjacentMountedSource(State actor, IAction action)
	{
		if (action is not IOrientedAction oriented)
			throw new ArgumentException($"Expected {nameof(IOrientedAction)}.", nameof(action));

		var frame = BodyFrame.From(actor);
		var direction = frame.Step(oriented.Orientation);
		return new AbilitySourcePose(
			actor.Position + direction,
			direction,
			Coord.Dot(direction, actor.Dorsal) == 0 ? actor.Dorsal : actor.Fore,
			oriented.Orientation);
	}

	private static AbilitySourcePose JumpDestination(State actor, IAction action)
	{
		var jump = Require<RoadsWeDontNeedRoadsAction>(action);
		var direction = BodyFrame.From(actor).HitFaceFrom(jump.Destination);
		return new AbilitySourcePose(jump.Destination, actor.Fore, actor.Dorsal, direction);
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

	private static AbilitySourcePose SelfOrientedSource(State actor, IAction action)
	{
		if (action is not IOrientedAction oriented)
			throw new ArgumentException($"Expected {nameof(IOrientedAction)}.", nameof(action));

		return new AbilitySourcePose(actor.Position, actor.Fore, actor.Dorsal, oriented.Orientation);
	}

	private static TAction Require<TAction>(IAction action)
		where TAction : IAction =>
		action is TAction typed
			? typed
			: throw new ArgumentException(
				$"Expected {typeof(TAction).Name}.",
				nameof(action));
}
