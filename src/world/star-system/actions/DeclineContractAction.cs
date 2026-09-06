using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record DeclineContractAction(string ActorId, string ContractId)
	: IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		DeclineContractDef.Instance;
}

public sealed class DeclineContractDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static DeclineContractDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is DeclineContractAction decline
		&& world.ContractRegistry.TryGet(decline.ContractId, out _)
		&& world.ContractRegistry.IsOffered(decline.ContractId)
		&& world.UnitRegistry.TryGet(decline.ActorId, out _);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var decline = (DeclineContractAction)action;
		return
		[
			new ActivateContractEffect(new ContractState(
				decline.ContractId,
				EContractStatus.Rejected,
				AcceptedAtTick: null,
				HolderUnitId: null,
				ContractState.EmptyBindings)),
		];
	}
}
