using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public record CompleteDeliveryFacilityLegAction(
	string ActorId,
	string PoiId,
	string FacilityId,
	string OperatorName,
	string ContractId,
	int LegIndex) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		CompleteDeliveryFacilityLegDef.Instance;
}

public class CompleteDeliveryFacilityLegDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static CompleteDeliveryFacilityLegDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is CompleteDeliveryFacilityLegAction turnIn
		&& world.FleetRegistry.TryGet(turnIn.ActorId, out _)
		&& world.ContractRegistry.TryGet(turnIn.ContractId, out var contract)
		&& world.ContractRegistry.TryGetState(turnIn.ContractId, out var state)
		&& state.Status == EContractStatus.Active
		&& state.HolderUnitId == turnIn.ActorId
		&& state is DeliveryContractState deliveryState
		&& contract.Objective is DeliveryObjective delivery
		&& turnIn.LegIndex == deliveryState.Progress.CurrentLegIndex
		&& turnIn.LegIndex >= 0
		&& turnIn.LegIndex < delivery.Route.Legs.Count
		&& delivery.Route.Legs[turnIn.LegIndex] is FacilityDeliveryLeg facility
		&& facility.PoiId == turnIn.PoiId
		&& facility.FacilityId == turnIn.FacilityId
		&& facility.OperatorName == turnIn.OperatorName
		&& !deliveryState.Progress.CompletedLegs[turnIn.LegIndex];

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var turnIn = (CompleteDeliveryFacilityLegAction)action;
		if (!IsLegal(turnIn, world, runtime))
			return [];

		return [new AdvanceDeliveryLegEffect(turnIn.ContractId, turnIn.LegIndex)];
	}
}
