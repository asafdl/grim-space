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
		&& world.FleetRegistry.TryGet(move.UnitId, out var unit)
		&& !WorkScheduler.HasAssignment(world, move.UnitId)
		&& !EngagementState.IsEngaged(unit.State);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var move = (MoveAction)action;
		var unit = world.FleetRegistry.FleetOf(move.UnitId);
		var origin = unit.State.PositionAt(world, runtime.CachedPath, 0).Position;

		var effects = new List<IEffect<StarMap, ActorRuntime>>
		{
			CancelPendingMoveEffect.Instance,
			new ClearPursueContactEffect(move.UnitId),
		};

		if (unit.State.Type == EType.PlayerFleet
			&& unit.State.CurrentEngagement?.Phase == EEngagementPhase.AwaitingDecision)
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
}
