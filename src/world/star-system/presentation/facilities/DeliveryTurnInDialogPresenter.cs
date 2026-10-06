using Godot;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Dialog;
using GrimSpace.World.StarSystem.Presentation.Scene;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed class DeliveryTurnInDialogPresenter
{
	private readonly StarSystemOrchestrator _orchestrator;
	private readonly string? _poiId;
	private readonly string? _facilityId;
	private readonly Button? _backButton;
	private readonly NpcDialogHudOverlay _hud;
	private FacilityOperator? _operator;
	private string? _meetingId;

	public DeliveryTurnInDialogPresenter(
		Node owner,
		Button backButton,
		StarSystemOrchestrator orchestrator,
		string poiId,
		string facilityId)
		: this(owner, backButton, orchestrator)
	{
		_poiId = poiId;
		_facilityId = facilityId;
	}

	public DeliveryTurnInDialogPresenter(
		Node owner,
		StarSystemOrchestrator orchestrator)
		: this(owner, null, orchestrator)
	{
	}

	private DeliveryTurnInDialogPresenter(
		Node owner,
		Button? backButton,
		StarSystemOrchestrator orchestrator)
	{
		_orchestrator = orchestrator;
		_backButton = backButton!;
		_hud = new NpcDialogHudOverlay();
		owner.AddChild(_hud);
		_hud.ChoiceSelected += OnChoiceSelected;
		_hud.Closed += OnHudClosed;
	}

	public bool IsOpen => _hud.IsOpen;

	public void OpenMeeting(string meetingId)
	{
		var active = FindMeetingContract(meetingId);
		if (active is null
			|| active.Definition.Objective is not DeliveryObjective delivery
			|| active.State is not DeliveryContractState deliveryState
			|| delivery.Route.Legs[deliveryState.Progress.CurrentLegIndex] is not SpaceMeetingDeliveryLeg meeting)
			return;

		_meetingId = meeting.MeetingId;
		_hud.Open(FacilityNpcDialogs.DeliveryTurnIn(
			meeting.ContactName,
			DeliveryLegMessageCatalog.Pick(
				active.Definition.Id,
				deliveryState.Progress.CurrentLegIndex,
				NextLegDestination(
					delivery.Route.Legs[deliveryState.Progress.CurrentLegIndex + 1])),
			isFinal: false));
		UpdateBackButton();
	}

	public void Open(FacilityOperator facilityOperator)
	{
		var active = FindTurnInContract(facilityOperator);
		if (active is null)
			return;

		_operator = facilityOperator;
		_meetingId = null;
		var delivery = (DeliveryObjective)active.Definition.Objective;
		var deliveryState = (DeliveryContractState)active.State;
		var isFinal = deliveryState.Progress.CurrentLegIndex == delivery.Route.Legs.Count - 1;
		_hud.Open(FacilityNpcDialogs.DeliveryTurnIn(
			facilityOperator,
			isFinal
				? active.Definition.Narrative.TurnInDialog
				: DeliveryLegMessageCatalog.Pick(
					active.Definition.Id,
					deliveryState.Progress.CurrentLegIndex,
					NextLegDestination(
						delivery.Route.Legs[deliveryState.Progress.CurrentLegIndex + 1])),
			isFinal));
		UpdateBackButton();
	}

	public bool TryHandleInput(InputEvent @event) => _hud.TryHandleInput(@event);

	private ActiveContract? FindTurnInContract(FacilityOperator facilityOperator) =>
		_orchestrator.Map.ContractRegistry
			.ActiveFor(State.PlayerFleetUnitId)
			.FirstOrDefault(active =>
				active.Definition.Objective is DeliveryObjective delivery
				&& active.State is DeliveryContractState deliveryState
				&& delivery.Route.Legs[deliveryState.Progress.CurrentLegIndex]
					is FacilityDeliveryLeg facility
				&& facility.PoiId == _poiId
				&& facility.FacilityId == _facilityId
				&& facility.OperatorName == facilityOperator.Name);

	private ActiveContract? FindMeetingContract(string meetingId) =>
		_orchestrator.Map.ContractRegistry
			.ActiveFor(State.PlayerFleetUnitId)
			.FirstOrDefault(active =>
				active.Definition.Objective is DeliveryObjective delivery
				&& active.State is DeliveryContractState deliveryState
				&& delivery.Route.Legs[deliveryState.Progress.CurrentLegIndex]
					is SpaceMeetingDeliveryLeg meeting
				&& meeting.MeetingId == meetingId);

	private void OnChoiceSelected(string choiceId)
	{
		if (_operator is null && _meetingId is null)
			return;

		if (choiceId == FacilityNpcDialogs.LeaveChoiceId)
		{
			_hud.Close();
			return;
		}

		if (choiceId != FacilityNpcDialogs.TurnInChoiceId)
			throw new InvalidOperationException($"Unknown delivery dialog choice '{choiceId}'.");

		var active = _meetingId is not null
			? FindMeetingContract(_meetingId)
			: FindTurnInContract(_operator!);
		if (active is null)
		{
			_hud.Close();
			return;
		}

		var delivery = (DeliveryContractState)active.State;
		var committed = _orchestrator.TryCommitPlayerInput(new CompleteDeliveryFacilityLegAction(
			State.PlayerFleetUnitId,
			_poiId ?? "",
			_facilityId ?? "",
			_operator?.Name ?? "",
			active.Definition.Id,
			delivery.Progress.CurrentLegIndex,
			_meetingId));
		if (!committed)
		{
			var speaker = _operator?.Name ?? "Delivery contact";
			if (_operator is null
				&& ((DeliveryObjective)active.Definition.Objective).Route.Legs[delivery.Progress.CurrentLegIndex]
					is SpaceMeetingDeliveryLeg meeting)
				speaker = meeting.ContactName;
			_hud.Open(FacilityNpcDialogs.DeliveryTurnIn(
				speaker,
				"Something's wrong with the handoff. Try again in a moment.",
				delivery.Progress.CurrentLegIndex
					== ((DeliveryObjective)active.Definition.Objective).Route.Legs.Count - 1));
			UpdateBackButton();
			return;
		}

		_hud.Close();
	}

	private void OnHudClosed()
	{
		_operator = null;
		_meetingId = null;
		MapNavigationContext.ClearActiveOperator();
		UpdateBackButton();
	}

	private string NextLegDestination(DeliveryLeg leg) =>
		leg switch
		{
			FacilityDeliveryLeg facility => _orchestrator.Map.PointsOfInterest
				.FirstOrDefault(poi => poi.Id == facility.PoiId) is { } poi
				? $"{poi.GetFacility(facility.FacilityId).DisplayName} at {poi.DisplayName}"
				: facility.PoiId,
			SpaceMeetingDeliveryLeg meeting => $"contact {meeting.ContactName}",
			_ => throw new ArgumentOutOfRangeException(nameof(leg), leg, null),
		};

	private void UpdateBackButton()
	{
		if (_backButton is not null)
			_backButton.Disabled = _hud.IsOpen;
	}
}
