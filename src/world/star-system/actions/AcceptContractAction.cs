using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record AcceptContractAction(string ActorId, string ContractId)
	: IAction<StarMap, ActorRuntime>
{
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
		&& IsAcceptLegal(accept, world);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var accept = (AcceptContractAction)action;
		if (!IsAcceptLegal(accept, world))
			throw new InvalidOperationException(
				$"Cannot accept contract '{accept.ContractId}' for actor '{accept.ActorId}'.");

		return [new AcceptContractEffect(accept.ContractId)];
	}

	private static bool IsAcceptLegal(AcceptContractAction accept, StarMap world)
	{
		if (!world.ContractRegistry.IsOffered(accept.ContractId))
			return false;

		if (!world.UnitRegistry.TryGet(accept.ActorId, out var unit))
			return false;

		if (!world.ContractRegistry.TryGet(accept.ContractId, out var contract))
			return false;

		if (contract.IssuerPoiId is null)
			return true;

		if (unit.State.Phase != EPhase.Docked || string.IsNullOrEmpty(unit.State.DockedAtDockId))
			return false;

		return world.DocksById[unit.State.DockedAtDockId].PoiId == contract.IssuerPoiId;
	}
}
