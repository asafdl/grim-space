using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record TurnInDeliveryAction(
	string ActorId,
	string PoiId,
	string FacilityId,
	string OperatorName,
	string ContractId) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		TurnInDeliveryDef.Instance;
}

public sealed class TurnInDeliveryDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static TurnInDeliveryDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is TurnInDeliveryAction turnIn
		&& world.FleetRegistry.TryGet(turnIn.ActorId, out _)
		&& world.ContractRegistry.TryGet(turnIn.ContractId, out var contract)
		&& world.ContractRegistry.TryGetState(turnIn.ContractId, out var state)
		&& state.Status == EContractStatus.Active
		&& state.HolderUnitId == turnIn.ActorId
		&& !state.DeliveryTurnedIn
		&& contract.Objective is DeliveryObjective delivery
		&& delivery.TurnInPoiId == turnIn.PoiId
		&& delivery.TurnInFacilityId == turnIn.FacilityId
		&& delivery.TurnInOperatorName == turnIn.OperatorName;

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var turnIn = (TurnInDeliveryAction)action;
		if (!IsLegal(turnIn, world, runtime))
			return [];

		return [new SetDeliveryTurnedInEffect(turnIn.ContractId)];
	}
}
