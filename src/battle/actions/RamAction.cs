using System.Collections.Frozen;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Actions;

public sealed record RamAction(
	string ActorId,
	ESpatialOrientation MountedOn = ESpatialOrientation.Forward)
	: IAction<BattleWorld, ActorRuntime>, IOrientedAction
{
	ESpatialOrientation IOrientedAction.Orientation => MountedOn;

	private static readonly string[] ActionTags = [BattleActionTags.Attack];

	IReadOnlyList<string> IAction.Tags => ActionTags;

	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		RamDef.Instance;
}

public sealed class RamDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IOrientedActionDef,
		IAreaActionDef
{
	public static RamDef Instance { get; } = new();

	public bool IsBlockable => false;

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var installed in world.StateOf(actorId).Loadout.InstalledAbilities)
		{
			if (installed.Spec.Kind != EAbilityKind.Ram)
				continue;

			var action = Bind(actorId, installed.MountedOn);
			if (IsLegal(action, world, runtime))
				yield return action;
		}
	}

	public RamAction Bind(
		string actorId,
		ESpatialOrientation mountedOn = ESpatialOrientation.Forward) =>
		new(actorId, mountedOn);

	IAction IOrientedActionDef.Bind(string actorId, ESpatialOrientation orientation) =>
		Bind(actorId, orientation);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(Cast(action), world, runtime);

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(RamAction action, BattleWorld world, ActorRuntime runtime) =>
		world.StateOf(action.ActorId).FindInstalled(EAbilityKind.Ram, action.MountedOn) is not null;

	public bool IsLegal(RamAction action, BattleWorld world, ActorRuntime runtime)
	{
		var state = world.StateOf(action.ActorId);
		if (BattleStatuses.HasOverheat(state))
			return false;

		var installed = state.FindInstalled(EAbilityKind.Ram, action.MountedOn);
		if (installed is null || state.MountRuntimeFor(installed.Mount).UsesRemaining <= 0)
			return false;

		return TryFindTarget(world, action.ActorId, out _, out _);
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		RamAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.Ram, action.MountedOn)
			?? throw new InvalidOperationException("Ram ability not installed for actor.");
		if (installed.Spec is not RamSpec ram)
			throw new InvalidOperationException("Ram spec missing.");
		if (!TryFindTarget(world, action.ActorId, out var targetId, out var destination, ram.Range))
			throw new InvalidOperationException("Ram has no legal target.");

		var impactCells = new HashSet<Coord> { world.StateOf(targetId).Position }.ToFrozenSet();
		return
		[
			new HeatChangeEffect(HeatChangeEffect.PerAbilityUse),
			new ApplyAreaDamageEffect(
				EImpactCause.RamImpact,
				impactCells,
				ram.Damage,
				state.Position),
			new MoveEffect(destination),
			new MountUsesChangeEffect(installed.Mount, -1),
		];
	}

	public IReadOnlySet<Coord> AffectedCells(RamAction action, BattleWorld world)
	{
		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.Ram, action.MountedOn);
		if (installed?.Spec is not RamSpec ram)
			return FrozenSet<Coord>.Empty;
		if (!TryFindTarget(world, action.ActorId, out var targetId, out _, ram.Range))
			return FrozenSet<Coord>.Empty;

		return new HashSet<Coord> { world.StateOf(targetId).Position }.ToFrozenSet();
	}

	IReadOnlySet<Coord> IAreaActionDef.AffectedCells(IAction action, BattleWorld world) =>
		AffectedCells(Cast(action), world);

	public static bool TryFindTarget(
		BattleWorld world,
		string actorId,
		out string targetId,
		out Coord destination,
		int range = -1)
	{
		targetId = null!;
		destination = default;
		var units = UnitRegistry.For(world);
		var actorUnit = units.UnitOf(actorId);
		var actor = actorUnit.State;
		if (range < 0)
		{
			var installed = actor.FindInstalled(EAbilityKind.Ram);
			range = installed?.Spec is RamSpec ram ? ram.Range : RamSpec.Baseline.Range;
		}

		var fore = actor.Fore;
		var land = actor.Position;

		for (var along = 1; along <= range; along++)
		{
			var cell = actor.Position + fore * along;
			if (!world.Grid.IsInBounds(cell))
				return false;

			foreach (var unit in units.All)
			{
				if (!unit.State.IsAlive || unit.State.Position != cell)
					continue;

				if (unit.Team == actorUnit.Team)
					return false;

				targetId = unit.State.Id;
				destination = land;
				return true;
			}

			if (world.BlockedCells.Contains(cell))
				return false;

			land = cell;
		}

		return false;
	}

	private static RamAction Cast(IAction action) =>
		action is RamAction typed
			? typed
			: throw new ArgumentException($"Expected {nameof(RamAction)}.", nameof(action));
}
