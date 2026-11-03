using System.Collections.Frozen;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Actions;

public sealed record HighSpeedRodAction(string ActorId, ESpatialOrientation MountedOn)
	: IAction<BattleWorld, ActorRuntime>, IMountedAction
{
	private static readonly string[] ActionTags = [BattleActionTags.Attack];
	IReadOnlyList<string> IAction.Tags => ActionTags;
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		HighSpeedRodDef.Instance;
}

public sealed class HighSpeedRodDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IMountedActionDef, IAreaActionDef
{
	public static HighSpeedRodDef Instance { get; } = new();
	public bool IsBlockable => true;

	public HighSpeedRodAction Bind(string actorId, ESpatialOrientation mountedOn) =>
		new(actorId, mountedOn);

	IAction IMountedActionDef.Bind(string actorId, ESpatialOrientation mountedOn) =>
		Bind(actorId, mountedOn);

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var installed in world.StateOf(actorId).Loadout.InstalledAbilities)
		{
			if (installed.Kind != EAbilityKind.HighSpeedRod)
				continue;
			var action = Bind(actorId, installed.MountedOn);
			if (IsLegal(action, world, runtime))
				yield return action;
		}
	}

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime)
	{
		var rod = Cast(action);
		return world.StateOf(rod.ActorId).FindInstalled(EAbilityKind.HighSpeedRod, rod.MountedOn) is not null;
	}

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime)
	{
		var rod = Cast(action);
		var state = world.StateOf(rod.ActorId);
		var installed = state.FindInstalled(EAbilityKind.HighSpeedRod, rod.MountedOn);
		return state.IsAlive
			&& !BattleStatuses.HasOverheat(state)
			&& installed is not null
			&& state.UsesRemaining(installed.Mount) > 0;
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action, BattleWorld world, ActorRuntime runtime)
	{
		var rod = Cast(action);
		var state = world.StateOf(rod.ActorId);
		var installed = state.FindInstalled(EAbilityKind.HighSpeedRod, rod.MountedOn)
			?? throw new InvalidOperationException("High Velocity Rod is not installed on this mount.");
		if (installed.Spec is not HighSpeedRodSpec spec)
			throw new InvalidOperationException("High Velocity Rod spec missing.");
		return
		[
			new HeatChangeEffect(HeatChangeEffect.PerAbilityUse),
			new ApplyAreaDamageEffect(
				EImpactCause.HighSpeedRod,
				AffectedCells(rod, world),
				spec.Damage,
				state.Position,
				hullBreachDamagePercent: spec.HullBreachDamagePercent),
			new MountUsesChangeEffect(installed.Mount, -1),
		];
	}

	public IReadOnlySet<Coord> AffectedCells(HighSpeedRodAction action, BattleWorld world)
	{
		var state = world.StateOf(action.ActorId);
		if (state.FindInstalled(EAbilityKind.HighSpeedRod, action.MountedOn)?.Spec is not HighSpeedRodSpec spec)
			return FrozenSet<Coord>.Empty;
		var step = BodyFrame.From(state).Step(action.MountedOn);
		var cells = new HashSet<Coord>();
		for (var distance = 1; distance <= spec.Range; distance++)
		{
			var cell = state.Position + step * distance;
			if (!world.Grid.IsInBounds(cell))
				break;
			if (world.UnitRegistry.All.Any(unit => unit.State.IsAlive && unit.State.Position == cell))
			{
				cells.Add(cell);
				break;
			}
			if (world.BlockedCells.Contains(cell) || world.AbilityBlockingCells.Contains(cell))
				break;
			cells.Add(cell);
		}
		return cells.ToFrozenSet();
	}

	IReadOnlySet<Coord> IAreaActionDef.AffectedCells(IAction action, BattleWorld world) =>
		AffectedCells(Cast(action), world);

	private static HighSpeedRodAction Cast(IAction action) =>
		action as HighSpeedRodAction
		?? throw new ArgumentException($"Expected {nameof(HighSpeedRodAction)}.", nameof(action));
}
