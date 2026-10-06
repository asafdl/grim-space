using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record FailDeliveryDeadlineAction(
	string ActorId,
	string ContractId) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		FailDeliveryDeadlineDef.Instance;
}

public sealed class FailDeliveryDeadlineDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static FailDeliveryDeadlineDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is FailDeliveryDeadlineAction fail
		&& string.Equals(fail.ActorId, StarSystemActorIds.Contracts, StringComparison.Ordinal)
		&& world.ContractRegistry.TryGetState(fail.ContractId, out var state)
		&& state.Status == EContractStatus.Active
		&& state is DeliveryContractState delivery
		&& delivery.Progress.DeadlineTick is int deadlineTick
		&& world.Timeline.Clock.Current > deadlineTick;

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		if (action is not FailDeliveryDeadlineAction fail
			|| !IsLegal(action, world, runtime))
			return [];

		return
		[
			new EndContractEffect(
				fail.ContractId,
				EContractStatus.Failed,
				EDeliveryFailureReason.Deadline),
		];
	}
}
