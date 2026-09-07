using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record HuntUnitAction(
	string ActorId,
	string TargetUnitId,
	Coord Destination,
	TransitPath Path) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		HuntUnitDef.Instance;
}

public sealed class HuntUnitDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static HuntUnitDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is HuntUnitAction hunt
		&& hunt.ActorId != hunt.TargetUnitId
		&& world.UnitRegistry.TryGet(hunt.ActorId, out var initiator)
		&& world.UnitRegistry.TryGet(hunt.TargetUnitId, out var target)
		&& target.State.CombatProfile is not null
		&& IsReadyToDepart(initiator.State);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var hunt = (HuntUnitAction)action;
		var unit = world.UnitRegistry.UnitOf(hunt.ActorId);
		var origin = ResolveOrigin(world, unit, runtime);

		return
		[
			CancelPendingMoveEffect.Instance,
			new SetEngagementIntentEffect(hunt.ActorId, hunt.TargetUnitId),
			..MovementEffects.BeginJourney(
				hunt.ActorId,
				runtime,
				world,
				origin,
				hunt.Destination,
				hunt.Path),
		];
	}

	private static bool IsReadyToDepart(State state) =>
		state.IsReadyToDepart
		|| state.Phase == EPhase.InTransit
		|| state is { ChoreDockIds.Count: 0, Phase: EPhase.Docked }
			&& !string.IsNullOrEmpty(state.DockedAtDockId);

	private static Coord ResolveOrigin(StarMap world, Unit unit, ActorRuntime runtime) =>
		MoveDef.ResolveOrigin(world, unit, runtime);
}
