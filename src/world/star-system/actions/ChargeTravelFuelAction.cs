using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record ChargeTravelFuelAction(
	string ActorId,
	int Tick) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		ChargeTravelFuelDef.Instance;
}

public sealed class ChargeTravelFuelDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static ChargeTravelFuelDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is ChargeTravelFuelAction charge
		&& charge.Tick == world.Timeline.Clock.Current
		&& world.FleetRegistry.TryGet(charge.ActorId, out var fleet)
		&& fleet.State.Type == EType.PlayerFleet
		&& fleet.State.Travel is FleetTravel.Journey
		&& runtime.CachedPath is not null;

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var charge = (ChargeTravelFuelAction)action;
		return TravelFuelEffects.ChargeWhileJourneying(world, runtime, charge.ActorId);
	}
}
