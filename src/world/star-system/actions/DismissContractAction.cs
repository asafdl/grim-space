using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record DismissContractAction(
	string ActorId,
	string ContractId) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		DismissContractDef.Instance;
}

public sealed class DismissContractDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static DismissContractDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is DismissContractAction dismiss
		&& world.FleetRegistry.TryGet(dismiss.ActorId, out _)
		&& world.ContractRegistry.TryGet(dismiss.ContractId, out var contract)
		&& !contract.IsStoryObjective
		&& world.ContractRegistry.TryGetState(dismiss.ContractId, out var state)
		&& state.Status == EContractStatus.Active
		&& state.HolderUnitId == dismiss.ActorId;

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var dismiss = (DismissContractAction)action;
		if (!IsLegal(dismiss, world, runtime))
			return [];

		return
		[
			new CleanupDismissedContractEffect(dismiss.ContractId, dismiss.ActorId),
			new EndContractEffect(dismiss.ContractId, EContractStatus.Failed),
		];
	}
}
