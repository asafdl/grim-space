using System.Collections.Frozen;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.PilotCapabilities;
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
	private static readonly string[] ActionTags =
	[BattleActionTags.Attack, BattleActionTags.ScrapDrone, BattleActionTags.Heal];

	IReadOnlyList<string> IAction.Tags => ActionTags;

	public IReadOnlyList<string> Tags => ActionTags;

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
		if (state.Overheat)
			return false;
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
		var swarm = installed.Spec as ScrapDroneSwarmSpec
			?? throw new InvalidOperationException("Scrap drone swarm spec missing.");
		var damage = ((IAreaDamage)swarm).Damage;
		IEffect<BattleWorld, ActorRuntime>[] effects =
		[
			new HeatChangeEffect(HeatChangeEffect.PerAbilityUse),
			new ApplyAreaDamageEffect(
				EImpactCause.ScrapDroneSwarmBurst,
				cells,
				damage,
				state.Position,
				alliesTakeDamage: false),
			new ApplyAreaAllyHealEffect(EImpactCause.ScrapDroneSwarmBurst, cells, swarm.Heal),
			new MountUsesChangeEffect(installed.Mount, -1),
		];
		return runtime.ActionEffects.Apply(action, world, runtime, effects);
	}

	public IReadOnlySet<Coord> AffectedCells(ScrapDroneSwarmAction action, BattleWorld world)
	{
		var state = world.StateOf(action.ActorId);
		var installed = state.FindInstalled(EAbilityKind.ScrapDroneSwarm, action.MountedOn);
		if (installed?.Spec is not IAreaDamage baseArea)
			return FrozenSet<Coord>.Empty;

		var areaDamage = HealingSkillRange.ApplyRangeBonus(baseArea, state, action.Tags);
		var frame = BodyFrame.From(state);
		var geometric = AbilityArea.CellsInBounds(areaDamage, frame, action.MountedOn, world.Grid);
		return AbilityArea.ApplyBlocking(state.Position, geometric, IsBlockable, world.AbilityBlockingCells);
	}

	IReadOnlySet<Coord> IAreaActionDef.AffectedCells(IAction action, BattleWorld world) =>
		AffectedCells(Cast(action), world);

	private static ScrapDroneSwarmAction Cast(IAction action) =>
		action as ScrapDroneSwarmAction ?? throw new ArgumentException($"Expected {nameof(ScrapDroneSwarmAction)}.", nameof(action));
}
