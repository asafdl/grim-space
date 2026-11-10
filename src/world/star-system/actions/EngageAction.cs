using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Run;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.World.StarSystem.Actions;

/// <param name="DeployedPilotIds">Player pilots to field; null fields every pilot with a ship.</param>
public sealed record EngageAction(string ActorId, IReadOnlyList<string>? DeployedPilotIds = null)
	: IAction<StarMap, ActorRuntime>
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
		&& IsValidDeployment(world, engage.DeployedPilotIds)
		&& TryResolveCounterparty(world, engage.ActorId, out _);

	private static bool IsValidDeployment(StarMap world, IReadOnlyList<string>? pilotIds)
	{
		if (pilotIds is null)
			return true;
		if (pilotIds.Count is 0 or > RunPilotRegistry.MaxDeployedPilots
			|| pilotIds.Distinct(StringComparer.Ordinal).Count() != pilotIds.Count
			|| world.PilotRegistry is not { } registry)
			return false;
		return pilotIds.All(id => registry.TryGet(id, out var pilot) && pilot.AssignedShipId is not null);
	}

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

		if (string.Equals(engage.ActorId, RunState.PlayerFleetUnitId, StringComparison.Ordinal)
			&& !WreckageQueries.TryGetAmbushContract(world, counterpartyId, out _))
		{
			var candidates = HazardPayrollAccounting.CandidatesFrom(world.PilotRegistry, engage.DeployedPilotIds);
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

		effects.Add(new CommitEngagementEffect(engage.ActorId, counterpartyId, refusedPilotIds, engage.DeployedPilotIds));
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
