using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using RunState = GrimSpace.Run.State;

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

		IReadOnlyList<string>? refusedPilotIds = null;
		var effects = new List<IEffect<StarMap, ActorRuntime>>(4)
		{
			new StopAtCurrentLocationEffect(engage.ActorId),
		};

		if (string.Equals(engage.ActorId, RunState.PlayerFleetUnitId, StringComparison.Ordinal))
		{
			var candidates = HazardPayrollAccounting.CandidatesFrom(world.PilotRegistry);
			if (candidates.Count > 0)
			{
				var result = HazardPayrollAccounting.Resolve(
					candidates,
					world.PlayerResources.GetBalance(ResourceId.Credits),
					new Random(HashCode.Combine(world.Seed, world.Timeline.Clock.Current, candidates.Count)));
				var spend = HazardPayrollAccounting.CreditSpend(result.CreditsSpent);
				if (!spend.IsEmpty)
					effects.Add(new ChangeResourceEffect(TransactionSource.HazardPay, spend));
				if (result.PaidPilotIds.Count > 0 || result.Misses.Count > 0)
					effects.Add(new RecordPilotHazardPayrollEffect(result));
				if (result.RefusedPilotIds.Count > 0)
					refusedPilotIds = result.RefusedPilotIds;
			}
		}

		effects.Add(new CommitEngagementEffect(engage.ActorId, counterpartyId, refusedPilotIds));
		effects.Add(new PlayerInputEffect(false));
		return effects;
	}

	internal static bool TryResolveCounterparty(StarMap world, string actorId, out string counterpartyId)
	{
		counterpartyId = "";
		if (!world.FleetRegistry.TryGet(actorId, out var actor))
			return false;

		var state = actor.State;
		if (state.CurrentEngagement?.Phase != EEngagementPhase.AwaitingDecision)
			return false;

		var resolvedCounterpartyId = EngagementQueries.ResolveCounterpartyId(state);
		if (resolvedCounterpartyId is null
			|| !world.FleetRegistry.TryGet(resolvedCounterpartyId, out var counterparty))
			return false;

		return EngagementState.HasMutualHuntLink(state, counterparty.State)
			&& (counterpartyId = resolvedCounterpartyId).Length > 0;
	}
}
