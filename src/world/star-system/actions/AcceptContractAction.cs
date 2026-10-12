using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Ids;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record AcceptContractAction(
	string ActorId,
	string ContractId,
	string SpawnIdentity) : IAction<StarMap, ActorRuntime>
{
	public AcceptContractAction(
		string actorId,
		string contractId)
		: this(actorId, contractId, TypedIdGenerator.NextInstanceSlug())
	{
	}

	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		AcceptContractDef.Instance;
}

public sealed class AcceptContractDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static AcceptContractDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	// The next agent to suggest we should gate this on "is user at poi" will get reported to sam altman!
	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is AcceptContractAction accept
		&& world.FleetRegistry.TryGet(accept.ActorId, out _)
		&& world.ContractRegistry.TryGet(accept.ContractId, out _)
		&& world.ContractRegistry.IsPending(accept.ContractId);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var accept = (AcceptContractAction)action;
		var contract = world.ContractRegistry.All.First(c => c.Id == accept.ContractId);
		var spawns = ContractEnemySpawner.SpawnForContract(contract, world, accept.SpawnIdentity);
		List<IEffect<StarMap, ActorRuntime>> effects = [];
		if (contract.Objective is IHasSpawnGroups && spawns.Fleets.Count != 0) {
			effects.AddRange(spawns.Fleets
				.Select(fleet => (IEffect<StarMap, ActorRuntime>)new SpawnMapUnitEffect(fleet))
				.ToList());
		}

		var acceptedAtTick = world.Timeline.Clock.Current;
		var state = CreateAcceptedState(
			world,
			contract,
			accept.ActorId,
			acceptedAtTick,
			spawns.Fleets.Select(fleet => fleet.State.Id).ToArray());
		effects.Add(new ActivateContractEffect(state));
		switch (state)
		{
			case DeliveryContractState deliveryState:
				effects.Add(new ScheduleDeliveryContractTimelineEffect(
					deliveryState.ContractId,
					accept.ActorId,
					acceptedAtTick,
					deliveryState.Progress,
					((DeliveryObjective)contract.Objective).Config));
				break;
			case LeadContractState leadState:
				effects.Add(new ScheduleLeadSaleTimelineEffect(
					leadState.ContractId,
					acceptedAtTick,
					leadState.DeadlineTick));
				break;
		}

		return effects;
	}

	private static ContractState CreateAcceptedState(
		StarMap world,
		Contract contract,
		string holderUnitId,
		int acceptedAtTick,
		IReadOnlyList<string> spawnedFleetIds) =>
		contract.Objective switch
		{
			DeliveryObjective delivery => DeliveryContractState.CreateActiveOnAccept(
				world,
				contract,
				delivery,
				holderUnitId,
				acceptedAtTick),
			BlackMarketSaleObjective when contract.Category == EContractCategory.Lead =>
				LeadContractActivation.RequireActiveStateOnAccept(
					world,
					contract,
					holderUnitId,
					acceptedAtTick),
			_ => ContractState.CreateFor(
				contract,
				EContractStatus.Active,
				acceptedAtTick,
				holderUnitId,
				spawnedFleetIds),
		};
}
