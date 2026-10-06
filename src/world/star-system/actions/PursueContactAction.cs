using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record PursueContactAction(
	string ActorId,
	ContactTarget Target,
	Coord Destination,
	TransitPath Path,
	EContactIntent Intent) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		PursueContactDef.Instance;
}

public sealed class PursueContactDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static PursueContactDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is PursueContactAction pursue
		&& world.FleetRegistry.TryGet(pursue.ActorId, out var initiator)
		&& initiator.State.CanMove
		&& pursue.Target switch
		{
			FleetContactTarget fleet =>
				pursue.Intent is EContactIntent.Engagement or EContactIntent.DeliveryMeeting
				&& pursue.ActorId != fleet.UnitId
				&& world.FleetRegistry.Contains(fleet.UnitId),
			WreckContactTarget wreck =>
				pursue.Intent == EContactIntent.WreckInvestigation
				&& WreckageQueries.IsActiveWreckContractForHolder(
					world,
					pursue.ActorId,
					wreck.ContractId),
			_ => false,
		};

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var pursue = (PursueContactAction)action;
		return ResolveEffects(pursue, world, runtime);
	}

	internal static IReadOnlyList<IEffect<StarMap, ActorRuntime>> ResolveEffects(
		PursueContactAction pursue,
		StarMap world,
		ActorRuntime runtime)
	{
		var unit = world.FleetRegistry.FleetOf(pursue.ActorId);
		var origin = MoveDef.ResolveOrigin(world, unit, runtime);

		var effects = new List<IEffect<StarMap, ActorRuntime>>
		{
			CancelPendingMoveEffect.Instance,
			new ClearPursueContactEffect(pursue.ActorId),
		};

		switch (pursue.Target)
		{
			case FleetContactTarget fleet:
				if (pursue.Intent == EContactIntent.Engagement)
					effects.Add(new SetEngagementIntentEffect(pursue.ActorId, fleet.UnitId));
				effects.Add(new SetTravelTargetEffect(
					pursue.ActorId,
					TravelTarget.Fleet(fleet.UnitId, pursue.Intent)));
				break;
			case WreckContactTarget wreck:
				effects.Add(new SetTravelTargetEffect(pursue.ActorId, TravelTarget.Wreck(wreck.ContractId)));
				break;
		}

		effects.AddRange(MovementEffects.BeginJourney(
			pursue.ActorId,
			runtime,
			world,
			origin,
			pursue.Destination,
			pursue.Path));

		return effects;
	}
}
