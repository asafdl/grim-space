using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Actions;

public sealed record ScrapDroneSwarmAction(
	string ActorId,
	ESpatialOrientation MountedOn) : IAction<BattleWorld, ActorRuntime>, IMountedAction
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		ScrapDroneSwarmDef.Instance;
}

public sealed class ScrapDroneSwarmDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IMountedActionDef,
		IAreaActionDef
{
	public static ScrapDroneSwarmDef Instance { get; } = new();

	public bool IsBlockable => false;

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var state = world.StateOf(actorId);
		foreach (var installed in state.Loadout.InstalledAbilities)
		{
			if (installed.Spec.Kind != EAbilityKind.ScrapDroneSwarm)
				continue;

			var action = Bind(actorId, installed.MountedOn);
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	public ScrapDroneSwarmAction Bind(string actorId, ESpatialOrientation mountedOn) =>
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

	public bool IsPossible(ScrapDroneSwarmAction action, BattleWorld world, ActorRuntime runtime) =>
		world.StateOf(action.ActorId).FindInstalled(EAbilityKind.ScrapDroneSwarm, action.MountedOn) is not null;

	public bool IsLegal(ScrapDroneSwarmAction action, BattleWorld world, ActorRuntime runtime)
	{
		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.ScrapDroneSwarm, action.MountedOn);
		if (installed is null || state.MountRuntimeFor(installed.Mount).UsesRemaining <= 0)
			return false;

		return IsPossible(action, world, runtime);
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		ScrapDroneSwarmAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var cells = AffectedCells(action, world);

		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.ScrapDroneSwarm, action.MountedOn)
			?? throw new InvalidOperationException("Scrap drone swarm ability not installed for actor.");
		var damage = installed.Spec is IAreaDamage area ? area.Damage : throw new InvalidOperationException("Scrap drone swarm spec missing area damage.");
		return
		[
			new ApplyAreaDamageEffect(
				EImpactCause.ScrapDroneSwarmBurst,
				cells,
				damage,
				state.Position),
			new MountUsesChangeEffect(installed.Mount, -1),
		];
	}

	public HashSet<Coord> AffectedCells(ScrapDroneSwarmAction action, BattleWorld world)
	{
		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.ScrapDroneSwarm, action.MountedOn);
		if (installed?.Spec is not IAreaDamage areaDamage)
			return [];

		var frame = BodyFrame.From(state);
		var geometric = AbilityArea.CellsInBounds(areaDamage, frame, action.MountedOn, world.Grid);
		return AbilityArea.ApplyBlocking(state.Position, geometric, IsBlockable, world.Hazards);
	}

	IReadOnlySet<Coord> IAreaActionDef.AffectedCells(IAction action, BattleWorld world) =>
		AffectedCells(Cast(action), world);

	private static ScrapDroneSwarmAction Cast(IAction action) =>
		action as ScrapDroneSwarmAction ?? throw new ArgumentException($"Expected {nameof(ScrapDroneSwarmAction)}.", nameof(action));
}
