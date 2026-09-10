using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record MoveAction(
	string ActorId,
	string UnitId,
	Coord Destination,
	TransitPath Path) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		MoveDef.Instance;
}

public sealed class MoveDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static MoveDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is MoveAction move
		&& world.UnitRegistry.TryGet(move.UnitId, out var unit)
		&& unit.State.CanMove
		&& unit.State.EngagedWithUnitIds.Count == 0
		&& !IsWaitingForScheduledWork(world, unit.State);

	private static bool IsWaitingForScheduledWork(StarMap world, State state) =>
		state.ChoreDockIds.Count > 0
		&& world.Timeline.ContainsPending(action =>
			action is BeginWorkAction begin && begin.UnitId == state.Id);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var move = (MoveAction)action;
		var unit = world.UnitRegistry.UnitOf(move.UnitId);
		var origin = ResolveOrigin(world, unit, runtime);

		var effects = new List<IEffect<StarMap, ActorRuntime>>
		{
			CancelPendingMoveEffect.Instance,
			new ClearEngagementIntentEffect(move.UnitId),
		};

		if (unit.State.Type == EType.PlayerFleet
			&& unit.State.EngagementPhase == EEngagementPhase.AwaitingDecision)
			effects.Add(new PlayerInputEffect(false));

		effects.AddRange(MovementEffects.BeginJourney(
			move.UnitId,
			runtime,
			world,
			origin,
			move.Destination,
			move.Path));

		return effects;
	}

	internal static Coord ResolveOrigin(StarMap world, Unit unit, ActorRuntime runtime)
	{
		var state = unit.State;
		if (state.Phase == EPhase.InTransit)
		{
			var path = runtime.CachedPath
				?? throw new InvalidOperationException(
					$"Unit '{state.Id}' is in transit without a cached path.");
			var elapsed = world.Timeline.Clock.Current - state.Journey.StartTick;
			return path.SampleAtElapsed(elapsed, state.SpeedPerTick).Position;
		}

		if (!string.IsNullOrEmpty(state.DockedAtDockId))
			return world.DocksById[state.DockedAtDockId].Position;

		return state.IdleCoord;
	}
}
