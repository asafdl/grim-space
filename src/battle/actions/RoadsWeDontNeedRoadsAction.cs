using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Actions;

public sealed record RoadsWeDontNeedRoadsAction(string ActorId, Coord Destination)
	: IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		RoadsWeDontNeedRoadsDef.Instance;
}

public sealed class RoadsWeDontNeedRoadsDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>, IAreaActionDef
{
	public static RoadsWeDontNeedRoadsDef Instance { get; } = new();
	public bool IsBlockable => false;

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var state = world.StateOf(actorId);
		if (state.FindInstalled(EAbilityKind.RoadsWeDontNeedRoads)?.Spec is not RoadsWeDontNeedRoadsSpec spec)
			yield break;
		foreach (var direction in Enum.GetValues<ESpatialOrientation>())
		{
			var step = BodyFrame.WorldAligned(state.Position).Step(direction);
			var action = new RoadsWeDontNeedRoadsAction(actorId, state.Position + step * spec.Range);
			if (IsLegal(action, world, runtime))
				yield return action;
		}
	}

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		world.StateOf(Cast(action).ActorId).FindInstalled(EAbilityKind.RoadsWeDontNeedRoads) is not null;

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime)
	{
		var jump = Cast(action);
		var state = world.StateOf(jump.ActorId);
		var installed = state.FindInstalled(EAbilityKind.RoadsWeDontNeedRoads);
		if (!state.IsAlive || BattleStatuses.HasOverheat(state)
			|| installed?.Spec is not RoadsWeDontNeedRoadsSpec spec
			|| state.CooldownRemaining(installed.Mount) > 0)
			return false;
		var delta = jump.Destination - state.Position;
		var axes = (delta.X != 0 ? 1 : 0) + (delta.Y != 0 ? 1 : 0) + (delta.Z != 0 ? 1 : 0);
		return axes == 1
			&& state.Position.ManhattanDistanceTo(jump.Destination) == spec.Range
			&& world.Grid.IsInBounds(jump.Destination)
			&& !world.IsCellBlocked(jump.Destination);
	}

	public IReadOnlySet<Coord> AffectedCells(IAction action, BattleWorld world) =>
		new HashSet<Coord> { Cast(action).Destination };

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action, BattleWorld world, ActorRuntime runtime)
	{
		var jump = Cast(action);
		var installed = world.StateOf(jump.ActorId).FindInstalled(EAbilityKind.RoadsWeDontNeedRoads)
			?? throw new InvalidOperationException("Roads? We Don't Need Roads is not installed.");
		if (installed.Spec is not RoadsWeDontNeedRoadsSpec spec)
			throw new InvalidOperationException("Roads? We Don't Need Roads spec missing.");
		return
		[
			new HeatChangeEffect(HeatChangeEffect.PerAbilityUse),
			new MoveEffect(jump.Destination),
			new MountCooldownEffect(installed.Mount, spec.CooldownTurns),
		];
	}

	private static RoadsWeDontNeedRoadsAction Cast(IAction action) =>
		action as RoadsWeDontNeedRoadsAction
		?? throw new ArgumentException($"Expected {nameof(RoadsWeDontNeedRoadsAction)}.", nameof(action));
}
