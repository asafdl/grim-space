using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Actions;

public sealed record RailgunAction(
	string ActorId,
	ESpatialOrientation MountedOn = ESpatialOrientation.Forward)
	: IAction<BattleWorld, ActorRuntime>, IMountedAction
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		RailgunDef.Instance;
}

public sealed class RailgunDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IMountedActionDef,
		IAreaActionDef
{
	public static RailgunDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var installed in world.StateOf(actorId).Loadout.InstalledAbilities)
		{
			if (installed.Spec.Kind != EAbilityKind.Railgun)
				continue;

			var action = Bind(actorId, installed.MountedOn);
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	public RailgunAction Bind(
		string actorId,
		ESpatialOrientation mountedOn = ESpatialOrientation.Forward) =>
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

	public bool IsPossible(RailgunAction action, BattleWorld world, ActorRuntime runtime)
		=> AffectedCells(action, world).Count > 0;

	public bool IsLegal(RailgunAction action, BattleWorld world, ActorRuntime runtime)
	{
		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.Railgun, action.MountedOn);
		if (installed is null || state.MountRuntimeFor(installed.Mount).UsesRemaining <= 0)
			return false;

		return IsPossible(action, world, runtime);
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		RailgunAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var cells = AffectedCells(action, world);

		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.Railgun, action.MountedOn)
			?? throw new InvalidOperationException("Railgun ability not installed for actor.");
		var damage = installed.Spec is IAreaDamage area ? area.Damage : throw new InvalidOperationException("Railgun spec missing area damage.");
		return
		[
			new ResolveHazardEffect(
				EHazardKind.RailgunBurst,
				cells,
				damage),
			new MountUsesChangeEffect(installed.Mount, -1),
		];
	}

	public HashSet<Coord> AffectedCells(RailgunAction action, BattleWorld world)
	{
		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.Railgun, action.MountedOn);
		if (installed?.Spec is not IAreaDamage areaDamage)
			return [];

		var frame = BodyFrame.From(state);
		return AbilityArea.CellsInBounds(areaDamage, frame, action.MountedOn, world.Grid.IsInBounds);
	}

	IReadOnlySet<Coord> IAreaActionDef.AffectedCells(IAction action, BattleWorld world) =>
		AffectedCells(Cast(action), world);

	private static RailgunAction Cast(IAction action) =>
		action as RailgunAction ?? throw new ArgumentException($"Expected {nameof(RailgunAction)}.", nameof(action));
}
