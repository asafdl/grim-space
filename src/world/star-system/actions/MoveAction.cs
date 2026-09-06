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

	// TODO: this needs a bit of rework, chores and docks should not effect legality status of actions
	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is MoveAction move
		&& world.UnitRegistry.TryGet(move.UnitId, out var unit)
		&& (unit.State.IsReadyToDepart
			|| unit.State.Phase == EPhase.InTransit
			|| unit.State is { ChoreDockIds.Count: 0, Phase: EPhase.Docked }
				&& !string.IsNullOrEmpty(unit.State.DockedAtDockId));

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var move = (MoveAction)action;
		var unit = world.UnitRegistry.UnitOf(move.UnitId);
		var state = unit.State;
		var origin = ResolveOrigin(world, unit, runtime);
		var journeyId = runtime.NextJourneyId();
		var startTick = world.Timeline.Clock.Current;
		var durationTicks = move.Path.DurationTicks(state.SpeedPerTick);
		var completion = new CompleteMoveAction(move.UnitId, move.UnitId, journeyId);

		return
		[
			CancelPendingMoveEffect.Instance,
			UpdateLocationEffect.BeginJourney(
				move.UnitId,
				journeyId,
				origin,
				move.Destination,
				startTick,
				move.Path),
			new ScheduleMoveCompletionEffect(durationTicks, completion),
		];
	}

	private static Coord ResolveOrigin(StarMap world, Unit unit, ActorRuntime runtime)
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
