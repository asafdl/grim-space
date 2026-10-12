using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record ExpireLeadSaleAction(
	string ActorId,
	string ContractId) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		ExpireLeadSaleDef.Instance;
}

public sealed class ExpireLeadSaleDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static ExpireLeadSaleDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is ExpireLeadSaleAction expire
		&& string.Equals(expire.ActorId, StarSystemActorIds.Contracts, StringComparison.Ordinal)
		&& world.ContractRegistry.TryGetState(expire.ContractId, out var state)
		&& state is LeadContractState lead
		&& state.Status == EContractStatus.Active
		&& world.Timeline.Clock.Current > lead.DeadlineTick;

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		if (action is not ExpireLeadSaleAction expire
			|| !IsLegal(expire, world, runtime))
			return [];

		return [new EndContractEffect(expire.ContractId, EContractStatus.Completed)];
	}
}
