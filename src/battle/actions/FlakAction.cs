using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Actions;

public sealed record FlakAction(
	string ActorId,
	ESpatialOrientation MountedOn) : IAction<BattleWorld, ActorRuntime>, IMountedAction
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		FlakDef.Instance;
}

public sealed class FlakDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IMountedActionDef,
		IAreaActionDef
{
	public static FlakDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var state = world.StateOf(actorId);
		foreach (var installed in state.Spec.InstalledAbilities)
		{
			if (installed.Spec.Kind != EAbilityKind.Flak)
				continue;

			var action = Bind(actorId, installed.MountedOn);
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	public FlakAction Bind(string actorId, ESpatialOrientation mountedOn) =>
		new(actorId, mountedOn);

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

	public bool IsPossible(FlakAction action, BattleWorld world, ActorRuntime runtime)
	{
		var installed = world.StateOf(action.ActorId).FindInstalled(
			EAbilityKind.Flak,
			action.MountedOn);
		if (installed is null)
			return false;

		return AffectedCells(action, world).Count > 0;
	}

	public bool IsLegal(FlakAction action, BattleWorld world, ActorRuntime runtime)
	{
		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.Flak, action.MountedOn);
		if (installed is null || state.MountRuntimeFor(installed.Mount).UsesRemaining <= 0)
			return false;

		return IsPossible(action, world, runtime);
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		FlakAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var cells = AffectedCells(action, world);

		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.Flak, action.MountedOn)
			?? throw new InvalidOperationException("Flak ability not installed for actor.");
		var damage = installed.Spec is IAreaDamage area ? area.Damage : throw new InvalidOperationException("Flak spec missing area damage.");
		return
		[
			new ResolveHazardEffect(
				EHazardKind.FlakBurst,
				cells,
				damage),
			new MountUsesChangeEffect(installed.Mount, -1),
		];
	}

	public HashSet<Coord> AffectedCells(FlakAction action, BattleWorld world)
	{
		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.Flak, action.MountedOn);
		if (installed?.Spec is not IAreaDamage areaDamage)
			return [];

		var frame = BodyFrame.From(state);
		return AbilityArea.CellsInBounds(areaDamage, frame, action.MountedOn, world.Grid.IsInBounds);
	}

	IReadOnlySet<Coord> IAreaActionDef.AffectedCells(IAction action, BattleWorld world) =>
		AffectedCells(Cast(action), world);

	private static FlakAction Cast(IAction action) =>
		action as FlakAction ?? throw new ArgumentException($"Expected {nameof(FlakAction)}.", nameof(action));
}
