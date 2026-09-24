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
	private readonly string _poiId;
	private readonly string _facilityId;
	private readonly Button _backButton;
	private readonly NpcDialogHudOverlay _hud;
	private FacilityOperator? _operator;

	public DeliveryTurnInDialogPresenter(
		Control owner,
		Button backButton,
		StarSystemOrchestrator orchestrator,
		string poiId,
		string facilityId)
	{
		_orchestrator = orchestrator;
		_poiId = poiId;
		_facilityId = facilityId;
		_backButton = backButton;

		_hud = new NpcDialogHudOverlay();
		owner.AddChild(_hud);
		_hud.ChoiceSelected += OnChoiceSelected;
		_hud.Closed += OnHudClosed;
	}

	public bool IsOpen => _hud.IsOpen;

	public void Open(FacilityOperator facilityOperator)
	{
		var contract = FindTurnInContract(facilityOperator);
		if (contract is null)
			return;

		_operator = facilityOperator;
		_hud.Open(FacilityNpcDialogs.DeliveryTurnIn(facilityOperator, contract.Narrative.TurnInDialog));
		UpdateBackButton();
	}

	public bool TryHandleInput(InputEvent @event) => _hud.TryHandleInput(@event);

	private Contract? FindTurnInContract(FacilityOperator facilityOperator) =>
		_orchestrator.Map.ContractRegistry
			.ActiveFor(State.PlayerFleetUnitId)
			.Select(active => active.Definition)
			.FirstOrDefault(contract =>
				contract.Objective is DeliveryObjective delivery
				&& delivery.TurnInOperatorName == facilityOperator.Name);

	private void OnChoiceSelected(string choiceId)
	{
		if (_operator is null)
			return;

		if (choiceId == FacilityNpcDialogs.LeaveChoiceId)
		{
			_hud.Close();
			return;
		}

		if (choiceId != FacilityNpcDialogs.TurnInChoiceId)
			throw new InvalidOperationException($"Unknown delivery dialog choice '{choiceId}'.");

		var contract = FindTurnInContract(_operator);
		if (contract is null)
		{
			_hud.Close();
			return;
		}

		var committed = _orchestrator.TryCommitPlayerInput(new TurnInDeliveryAction(
			State.PlayerFleetUnitId,
			_poiId,
			_facilityId,
			_operator.Name,
			contract.Id));
		if (!committed)
		{
			_hud.Open(FacilityNpcDialogs.DeliveryTurnIn(
				_operator,
				"Something's wrong with the handoff. Try again in a moment."));
			UpdateBackButton();
			return;
		}

		_hud.Close();
	}

	private void OnHudClosed()
	{
		_operator = null;
		MapNavigationContext.ClearActiveOperator();
		UpdateBackButton();
	}

	private void UpdateBackButton() => _backButton.Disabled = _hud.IsOpen;
}
