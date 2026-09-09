using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record EngageAction(string ActorId) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		EngageDef.Instance;
}

public sealed class EngageDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static EngageDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is EngageAction engage
		&& TryResolveCounterparty(world, engage.ActorId, out _);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var engage = (EngageAction)action;
		if (!TryResolveCounterparty(world, engage.ActorId, out var counterpartyId))
			return [];

		return
		[
			new StopAtCurrentLocationEffect(engage.ActorId),
			new CommitEngagementEffect(engage.ActorId, counterpartyId),
		];
	}

	internal static bool TryResolveCounterparty(StarMap world, string actorId, out string counterpartyId)
	{
		counterpartyId = "";
		if (!world.UnitRegistry.TryGet(actorId, out var actor))
			return false;

		var state = actor.State;
		if (state.EngagementPhase != EEngagementPhase.AwaitingDecision)
			return false;

		var resolvedCounterpartyId = EngagementQueries.ResolveCounterpartyId(state);
		if (resolvedCounterpartyId is null
			|| !world.UnitRegistry.TryGet(resolvedCounterpartyId, out var counterparty))
			return false;

		return (counterparty.State.HuntedByUnitId == actorId
				|| state.HuntedByUnitId == counterpartyId)
			&& (counterpartyId = resolvedCounterpartyId).Length > 0;
	}
}
