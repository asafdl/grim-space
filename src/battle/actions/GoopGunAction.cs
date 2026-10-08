using System.Collections.Frozen;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Ids;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Actions;

public sealed record GoopGunAction(
	string ActorId,
	ESpatialOrientation MountedOn,
	string GoopHazardId) : IAction<BattleWorld, ActorRuntime>, IMountedAction
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		GoopGunDef.Instance;
}

public sealed class GoopGunDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IMountedActionDef,
		IAreaActionDef
{
	public static GoopGunDef Instance { get; } = new();

	public bool IsBlockable => true;

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var goopHazardId = TypedIdGenerator.NextId(NonUnitTypeSlug.Goop);
		foreach (var action in Discover(actorId, goopHazardId, world))
		{
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	internal IEnumerable<GoopGunAction> Discover(string actorId, string goopHazardId, BattleWorld world)
	{
		var state = world.StateOf(actorId);
		foreach (var installed in state.Loadout.InstalledAbilities)
		{
			if (installed.Spec.Kind != EAbilityKind.GoopGun)
				continue;

			yield return Bind(actorId, installed.MountedOn, goopHazardId);
		}
	}

	public GoopGunAction Bind(string actorId, ESpatialOrientation mountedOn) =>
		Bind(actorId, mountedOn, TypedIdGenerator.NextId(NonUnitTypeSlug.Goop));

	public GoopGunAction Bind(string actorId, ESpatialOrientation mountedOn, string goopHazardId) =>
		new(actorId, mountedOn, goopHazardId);

	IAction IMountedActionDef.Bind(string actorId, ESpatialOrientation mountedOn) =>
		Bind(actorId, mountedOn);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(Cast(action), world, runtime);

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(GoopGunAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (string.IsNullOrWhiteSpace(action.GoopHazardId))
			return false;

		return world.StateOf(action.ActorId).FindInstalled(EAbilityKind.GoopGun, action.MountedOn) is not null;
	}

	public bool IsLegal(GoopGunAction action, BattleWorld world, ActorRuntime runtime)
	{
		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.GoopGun, action.MountedOn);
		if (installed is null || state.MountRuntimeFor(installed.Mount).CooldownRemaining > 0)
			return false;

		return IsPossible(action, world, runtime) && AffectedCells(action, world).Count > 0;
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		GoopGunAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.GoopGun, action.MountedOn)
			?? throw new InvalidOperationException("Goop gun not installed for actor.");
		if (installed.Spec is not GoopGunSpec goopGun)
			throw new InvalidOperationException("Goop gun spec missing.");

		var frame = BodyFrame.From(state);
		var cells = AffectedCells(action, world).ToFrozenSet();
		if (cells.Count == 0)
			throw new InvalidOperationException("Goop gun has no affected cells.");

		var burstDirection = frame.Step(action.MountedOn);
		var center = state.Position + burstDirection * goopGun.Range;
		var hazardFrame = BodyFrame.PatchAt(center, burstDirection, frame.Dorsal);

		var hazard = new GoopHazard
		{
			Id = action.GoopHazardId,
			ActorId = action.ActorId,
			Center = center,
			Frame = hazardFrame,
			Cells = cells,
		};

		return
		[
			new AddHazardEffect(hazard),
			new MountCooldownEffect(installed.Mount, goopGun.UnavailableTurns + 1),
			new ScheduleActionEffect(2, new RemoveHazardAction(action.ActorId, action.GoopHazardId)),
		];
	}

	public IReadOnlySet<Coord> AffectedCells(GoopGunAction action, BattleWorld world)
	{
		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.GoopGun, action.MountedOn);
		if (installed?.Spec is not IAreaDamage areaDamage)
			return FrozenSet<Coord>.Empty;

		var frame = BodyFrame.From(state);
		var geometric = AbilityArea.CellsInBounds(areaDamage, frame, action.MountedOn, world.Grid);
		return AbilityArea.ApplyBlocking(state.Position, geometric, IsBlockable, world.AbilityBlockingCells);
	}

	IReadOnlySet<Coord> IAreaActionDef.AffectedCells(IAction action, BattleWorld world) =>
		AffectedCells(Cast(action), world);

	private static GoopGunAction Cast(IAction action) =>
		action as GoopGunAction ?? throw new ArgumentException($"Expected {nameof(GoopGunAction)}.", nameof(action));
}
