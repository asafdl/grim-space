using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Ids;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record AcceptContractAction(
	string ActorId,
	string PoiId,
	string FacilityId,
	string OperatorName,
	string ContractId,
	string SpawnIdentity) : IAction<StarMap, ActorRuntime>
{
	public AcceptContractAction(
		string actorId,
		string poiId,
		string facilityId,
		string operatorName,
		string contractId)
		: this(actorId, poiId, facilityId, operatorName, contractId, TypedIdGenerator.NextInstanceSlug())
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

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is AcceptContractAction accept
		&& world.FleetRegistry.TryGet(accept.ActorId, out _)
		&& world.ContractRegistry.TryGet(accept.ContractId, out _)
		&& world.ContractRegistry.IsOffered(accept.ContractId);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var accept = (AcceptContractAction)action;
		var contract = world.ContractRegistry.All.First(c => c.Id == accept.ContractId);
		var spawns = Factory.Create(contract, world, accept.SpawnIdentity);
		var effects = spawns.Fleets
			.Select(fleet => (IEffect<StarMap, ActorRuntime>)new SpawnMapUnitEffect(fleet))
			.ToList();

		var state = new ContractState(
			accept.ContractId,
			EContractStatus.Active,
			world.Timeline.Clock.Current,
			accept.ActorId,
			spawns.Bindings);
		effects.Add(new ActivateContractEffect(state));
		return effects;
	}
}
