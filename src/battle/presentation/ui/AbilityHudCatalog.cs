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
	public sealed record Spec(
		EPlayerMode Mode,
		IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Def,
		AbilityTargetingSpec Targeting,
		string? IconPath,
		string Tooltip,
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
		state.Type == EType.Torpedo
			? [Resolve(DetonateDef.Instance)]
			: Capabilities.AbilityDefsForLoadout(state.Spec.InstalledAbilities)
				.Select(Resolve)
				.ToList();

	public static IReadOnlyList<Spec> ForDisplayState(UnitDisplayState unit) =>
		ForActor(unit.ToState());

	public static AbilityBarSlotState BuildState(Spec spec, UnitDisplayState unit, AbilityLegality legality) =>
		new(spec.Tooltip, spec.Charges(unit, legality), spec.IsLegal(legality));

	private static Spec Resolve(
		IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> def) =>
		def switch
		{
			FlakDef => new(
				EPlayerMode.Flak,
				def,
				new AbilityTargetingSpec(
					AdjacentMountedSource,
					AbilitySourceMeshes.CreateFlakBurst,
					new Color(0.96f, 0.64f, 0.2f, 0.44f)),
				"res://assets/ui/abilities/flak.svg",
				BattleHudCopy.FlakTooltip,
				(unit, _) => BattleHudCopy.Charges(
					unit.UsesRemaining(EAbilityKind.Flak),
					unit.MaxUsesPerTurn(EAbilityKind.Flak)),
				legality => legality.Weapons.IsKindLegal(EWeaponKind.Flak)),
			RailgunDef => new(
				EPlayerMode.Railgun,
				def,
				new AbilityTargetingSpec(
					ForwardSource<RailgunAction>,
					AbilitySourceMeshes.CreateRailgun,
					new Color(0.55f, 0.82f, 1f, 0.42f)),
				"res://assets/ui/abilities/railgun.svg",
				BattleHudCopy.RailgunTooltip,
				(unit, _) => BattleHudCopy.Charges(
					unit.UsesRemaining(EAbilityKind.Railgun),
					unit.MaxUsesPerTurn(EAbilityKind.Railgun)),
				legality => legality.Weapons.IsKindLegal(EWeaponKind.Railgun)),
			TorpedoDef => new(
				EPlayerMode.Torpedo,
				def,
				new AbilityTargetingSpec(
					TorpedoSource,
					AbilitySourceMeshes.CreateTorpedo,
					new Color(0.25f, 0.85f, 0.95f, 0.55f)),
				"res://assets/ui/abilities/torpedo.svg",
				BattleHudCopy.TorpedoTooltip,
				(unit, _) => BattleHudCopy.Charges(
					unit.ReadyMounts(EAbilityKind.TorpedoLauncher),
					unit.MountCount(EAbilityKind.TorpedoLauncher)),
				legality => legality.Weapons.IsKindLegal(EWeaponKind.Torpedo)),
			DetonateDef => new(
				EPlayerMode.Detonate,
				def,
				new AbilityTargetingSpec(
					SelfSource<DetonateAction>,
					AbilitySourceMeshes.CreateDetonation,
					new Color(1f, 0.42f, 0.18f, 0.5f)),
				"res://assets/ui/abilities/detonate.svg",
				BattleHudCopy.DetonateTooltip,
				(unit, _) => BattleHudCopy.Charges(
					unit.FuelRemaining,
					TorpedoBodySpec.Require(unit.Spec).FuelTurns),
				legality => legality.Detonate),
			SpawnPatrolDef => new(
				EPlayerMode.SpawnPatrol,
				def,
				new AbilityTargetingSpec(
					PatrolSource,
					AbilitySourceMeshes.CreatePatrol,
					new Color(0.4f, 0.9f, 0.58f, 0.48f)),
				"res://assets/ui/abilities/patrol.svg",
				BattleHudCopy.SpawnPatrolTooltip,
				(unit, _) => BattleHudCopy.Charges(
					unit.ReadyMounts(EAbilityKind.PatrolBay),
					unit.MountCount(EAbilityKind.PatrolBay)),
				legality => legality.SpawnPatrol),
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

	private static AbilitySourcePose TorpedoSource(State actor, IAction action)
	{
		var torpedo = Require<TorpedoAction>(action);
		var pose = TorpedoMount.LaunchPose(actor, torpedo.MountedOn);
		return new AbilitySourcePose(
			pose.Position,
			pose.Fore,
			pose.Dorsal,
			torpedo.MountedOn);
	}

	private static AbilitySourcePose PatrolSource(State actor, IAction action)
	{
		var patrol = Require<SpawnPatrolAction>(action);
		var pose = PatrolBayMount.LaunchPose(actor, patrol.MountedOn);
		return new AbilitySourcePose(
			pose.Position,
			pose.Fore,
			pose.Dorsal,
			patrol.MountedOn);
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
