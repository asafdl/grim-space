using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record CompleteContractAction(string ActorId, string ContractId)
	: IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		CompleteContractDef.Instance;
}

public sealed class CompleteContractDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static CompleteContractDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is CompleteContractAction complete
		&& world.ContractRegistry.TryGet(complete.ContractId, out var contract)
		&& world.ContractRegistry.TryGetState(complete.ContractId, out var state)
		&& state.Status == EContractStatus.Active
		&& state.HolderUnitId == complete.ActorId
		&& ContractFulfillment.IsFulfilled(world, new ActiveContract(contract, state));

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var complete = (CompleteContractAction)action;
		return [new CompleteContractEffect(complete.ContractId)];
	}
}
