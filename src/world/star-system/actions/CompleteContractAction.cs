using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record CompleteContractAction(
	string ActorId,
	string ContractId,
	ResourceBundle Payment) : IAction<StarMap, ActorRuntime>
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
		&& PaymentMatches(contract.Terms.Payment, complete.Payment)
		&& ContractFulfillment.IsFulfilled(world, new ActiveContract(contract, state));

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var complete = (CompleteContractAction)action;
		if (!IsLegal(complete, world, runtime))
			return [];

		var effects = new List<IEffect<StarMap, ActorRuntime>>
		{
			new CompleteContractEffect(complete.ContractId),
		};
		if (!complete.Payment.IsEmpty)
			effects.Add(new ChangeResourceEffect(TransactionSource.ContractPayment, complete.Payment));

		return effects;
	}

	private static bool PaymentMatches(ResourceBundle expected, ResourceBundle actual)
	{
		if (expected.IsEmpty)
			return actual.IsEmpty;

		foreach (var (id, amount) in expected)
		{
			if (!actual.TryGet(id, out var actualAmount) || actualAmount != amount)
				return false;
		}

		foreach (var (id, _) in actual)
		{
			if (!expected.TryGet(id, out _))
				return false;
		}

		return true;
	}
}
