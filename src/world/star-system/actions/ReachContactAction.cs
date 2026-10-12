using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record ReachContactAction(
	string InitiatorId,
	string TargetId,
	EContactIntent Intent = EContactIntent.Engagement)
	: IAction<StarMap, ActorRuntime>
{
	public string ActorId => InitiatorId;

	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		ReachContactDef.Instance;
}

public sealed class ReachContactDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static ReachContactDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is ReachContactAction reach
		&& world.FleetRegistry.TryGet(reach.InitiatorId, out var initiator)
		&& world.FleetRegistry.TryGet(reach.TargetId, out var target)
		&& (reach.Intent switch
		{
			EContactIntent.Engagement =>
				initiator.State.TravelTarget.MatchesFleet(
					reach.TargetId,
					EContactIntent.Engagement)
				&& initiator.State.CurrentEngagement?.Phase == EEngagementPhase.Pursuing
				&& initiator.State.CurrentEngagement?.Hunting == reach.TargetId
				&& !EngagementState.IsEngaged(initiator.State),
			EContactIntent.DeliveryMeeting or EContactIntent.BlackMarketMeeting =>
				IsActiveMeeting(world, reach.InitiatorId, target, reach.Intent),
			_ => false,
		});

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var reach = (ReachContactAction)action;
		if (reach.Intent is EContactIntent.DeliveryMeeting or EContactIntent.BlackMarketMeeting)
			return
			[
				new StopAtCurrentLocationEffect(reach.InitiatorId),
				new SetTravelTargetEffect(reach.InitiatorId, TravelTarget.None),
				new PlayerInputEffect(true),
			];

		var effects = new List<IEffect<StarMap, ActorRuntime>>
		{
			new StopAtCurrentLocationEffect(reach.InitiatorId),
			new SetTravelTargetEffect(reach.InitiatorId, TravelTarget.None),
			new ReachContactEffect(reach.InitiatorId, reach.TargetId),
		};

		if (world.FleetRegistry.TryGet(reach.InitiatorId, out var initiator)
			&& (initiator.State.Type == EType.PlayerFleet
				|| world.FleetRegistry.TryGet(reach.TargetId, out var target)
					&& target.State.Type == EType.PlayerFleet))
			effects.Add(new PlayerInputEffect(true));

		return effects;
	}

	internal static bool IsActiveMeeting(
		StarMap world,
		string initiatorId,
		Fleet target,
		EContactIntent intent) =>
		intent switch
		{
			EContactIntent.DeliveryMeeting =>
				IsActiveDeliveryMeeting(world, initiatorId, target),
			EContactIntent.BlackMarketMeeting =>
				IsActiveBlackMarketMeeting(world, initiatorId, target),
			_ => false,
		};

	private static bool IsActiveDeliveryMeeting(
		StarMap world,
		string initiatorId,
		Fleet target)
	{
		if (target.State.SourceContractId is not { } contractId
			|| !world.ContractRegistry.TryGet(contractId, out var contract)
			|| !world.ContractRegistry.TryGetState(contractId, out var state)
			|| state is not DeliveryContractState deliveryState
			|| deliveryState.Status != EContractStatus.Active
			|| deliveryState.HolderUnitId != initiatorId
			|| contract.Objective is not DeliveryObjective delivery)
			return false;

		return delivery.Route.Legs[deliveryState.Progress.CurrentLegIndex]
			is SpaceMeetingDeliveryLeg meeting
			&& meeting.MeetingId == target.State.Id;
	}

	private static bool IsActiveBlackMarketMeeting(
		StarMap world,
		string initiatorId,
		Fleet target)
	{
		if (target.State.SourceContractId is not { } contractId
			|| !world.ContractRegistry.TryGet(contractId, out var contract)
			|| contract.Category != EContractCategory.Lead
			|| !world.ContractRegistry.TryGetState(contractId, out var state)
			|| state is not LeadContractState lead
			|| lead.Status != EContractStatus.Active
			|| lead.HolderUnitId != initiatorId
			|| contract.Objective is not BlackMarketSaleObjective sale
			|| sale.Destination is not BlackMarketSpaceMeetingDestination meeting)
			return false;

		return meeting.MeetingId == target.State.Id;
	}
}
