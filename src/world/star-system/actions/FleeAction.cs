using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record FleeAction(string ActorId) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		FleeDef.Instance;
}

public sealed class FleeDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static FleeDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is FleeAction flee
		&& EngageDef.TryResolveCounterparty(world, flee.ActorId, out _)
		&& (flee.ActorId != State.PlayerFleetUnitId
			|| world.PlayerResources.GetBalance(ResourceId.Fuel) >= FuelCost(world));

	public static int FuelCost(StarMap world) =>
		world.PilotRegistry?.All.Sum(pilot =>
			StrategicEconomyConfig.FleeFuelForLevel(
				PilotProgression.LevelFor(pilot.ExperiencePoints))) ?? 0;

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var flee = (FleeAction)action;
		if (!EngageDef.TryResolveCounterparty(world, flee.ActorId, out var counterpartyId))
			return [];

		var effects = new List<IEffect<StarMap, ActorRuntime>>();
		if (flee.ActorId == State.PlayerFleetUnitId)
		{
			var fuelCost = FuelCost(world);
			if (fuelCost > 0)
			{
				// Pay before stopping settles travel fuel, so it cannot consume the escape budget.
				effects.Add(new ChangeResourceEffect(
					TransactionSource.FleeFuel,
					ResourceBundle.Of(ResourceId.Fuel, -fuelCost)));
			}
		}

		effects.Add(new StopAtCurrentLocationEffect(flee.ActorId));
		effects.Add(new ClearPursueContactEffect(flee.ActorId));
		effects.Add(new FleeEngagementEffect(flee.ActorId, counterpartyId));
		effects.Add(new PlayerInputEffect(false));

		if (TryResolveDeliveryInterception(world, flee.ActorId, counterpartyId, out var contractId))
		{
			effects.Add(new EndContractEffect(
				contractId,
				EContractStatus.Failed,
				EDeliveryFailureReason.FledInterceptor));
		}

		return effects;
	}

	private static bool TryResolveDeliveryInterception(
		StarMap world,
		string fleeingFleetId,
		string counterpartyId,
		out string contractId)
	{
		contractId = "";
		if (!world.FleetRegistry.TryGet(counterpartyId, out var counterparty)
			|| counterparty.State.PursuitDirective is not { } directive
			|| !string.Equals(
				directive.TargetFleetId,
				fleeingFleetId,
				StringComparison.Ordinal)
			|| !world.ContractRegistry.TryGetState(directive.ContractId, out var state)
			|| state.Status != EContractStatus.Active
			|| state is not DeliveryContractState delivery
			|| delivery.Progress.InterceptionState != EDeliveryInterceptionState.Assigned
			|| !string.Equals(
				delivery.Progress.InterceptorFleetId,
				counterpartyId,
				StringComparison.Ordinal))
			return false;

		contractId = directive.ContractId;
		return true;
	}
}
