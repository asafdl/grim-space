using Godot;
using GrimSpace.Components;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Dialog;
using GrimSpace.World.StarSystem.Presentation.Scene;
using GrimSpace.World.StarSystem.Presentation.Ui;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed class FacilityContractHudPresenter
{
	private readonly FacilitySceneBinding _binding;
	private readonly ContractHudOverlay _contractHud;
	private readonly NpcDialogHudOverlay _leadDialog;
	private readonly Action _stateChanged;
	private string? _activeLeadId;
	private string _leadSpeakerName = "";

	public FacilityContractHudPresenter(
		Control owner,
		FacilitySceneBinding binding,
		Action stateChanged)
	{
		ArgumentNullException.ThrowIfNull(owner);
		ArgumentNullException.ThrowIfNull(binding);
		ArgumentNullException.ThrowIfNull(stateChanged);

		_binding = binding;
		_stateChanged = stateChanged;
		var layer = new CanvasLayer { Layer = 20 };
		owner.AddChild(layer);
		_contractHud = new ContractHudOverlay();
		_contractHud.AcceptRequested += OnAcceptRequested;
		_contractHud.DeclineRequested += OnDeclineRequested;
		_contractHud.Closed += OnClosed;
		layer.AddChild(_contractHud);

		_leadDialog = new NpcDialogHudOverlay();
		_leadDialog.ChoiceSelected += OnLeadChoiceSelected;
		_leadDialog.Closed += OnClosed;
		owner.AddChild(_leadDialog);
	}

	public bool IsOpen => _contractHud.IsOpen || _leadDialog.IsOpen;

	public void Open(
		FacilityOperator facilityOperator,
		OperatorInteraction interaction)
	{
		if (!_binding.TryVisitContractOperator(facilityOperator.Name))
		{
			GD.PushError(
				$"Unable to pause contract placement for operator '{facilityOperator.Name}' " +
				$"at POI '{_binding.PoiId}'.");
			return;
		}

		if (interaction.Role == EFacilityOperatorRole.LeadContact)
			OpenLeadDialog(facilityOperator);
		else
			_contractHud.Open(
				_binding.Map,
				_binding.PoiId,
				_binding.FacilityId,
				facilityOperator.Name,
				OperatorDisplayLabels.Title(facilityOperator, interaction));
		_stateChanged();
	}

	public void SyncMap() => _contractHud.SyncMap(_binding.Map);

	public bool TryHandleInput(InputEvent @event) => _leadDialog.TryHandleInput(@event);

	private void OnAcceptRequested(string contractId)
	{
		var category = CategoryOf(contractId);
		if (!_binding.Intents.TryAcceptContract(contractId))
		{
			_contractHud.ShowError($"Unable to accept {Noun(category)}.");
			_stateChanged();
			return;
		}

		_contractHud.SyncMap(_binding.Map);
		_contractHud.ShowConfirmation("Contract accepted.", HudStatusKind.Success);
		_stateChanged();
	}

	private void OnDeclineRequested(string contractId)
	{
		var category = CategoryOf(contractId);
		if (!_binding.Intents.TryDeclineContract(contractId))
		{
			_contractHud.ShowError($"Unable to decline {Noun(category)}.");
			_stateChanged();
			return;
		}

		_contractHud.SyncMap(_binding.Map);
		_contractHud.ShowConfirmation("Contract declined.", HudStatusKind.Error);
		_stateChanged();
	}

	private void OpenLeadDialog(FacilityOperator facilityOperator)
	{
		if (!_binding.Map.TryGetPointOfInterest(_binding.PoiId, out var poi)
			|| !poi.OperatorTemporaryRoles.TryGetAssignment(
				_binding.FacilityId,
				facilityOperator.Name,
				out var assignment)
			|| assignment.Role != EFacilityOperatorRole.LeadContact
			|| assignment.SourceIds.Count != 1
			|| !_binding.Map.ContractRegistry.TryGet(assignment.SourceIds[0], out var lead)
			|| lead.Category != EContractCategory.Lead
			|| !_binding.Map.ContractRegistry.IsPending(lead.Id))
		{
			GD.PushError(
				$"Lead contact '{facilityOperator.Name}' does not have exactly one pending lead.");
			return;
		}

		_activeLeadId = lead.Id;
		_leadSpeakerName = facilityOperator.Name;
		_leadDialog.Open(FacilityNpcDialogs.BlackMarketLead(
			_leadSpeakerName,
			LeadContactMessageCatalog.Pick(lead, _binding.Map)));
	}

	private void OnLeadChoiceSelected(string choiceId)
	{
		if (choiceId == FacilityNpcDialogs.LeaveChoiceId)
		{
			_leadDialog.Close();
			return;
		}

		if (_activeLeadId is not { } leadId)
			return;

		switch (choiceId)
		{
			case FacilityNpcDialogs.AcceptLeadChoiceId:
				if (_binding.Intents.TryAcceptContract(leadId))
				{
					_activeLeadId = null;
					_leadDialog.Close();
					_stateChanged();
					return;
				}

				ShowLeadFailure("I can't point you to that sale anymore.");
				break;
			case FacilityNpcDialogs.RejectLeadChoiceId:
				if (_binding.Intents.TryDeclineContract(leadId))
				{
					_activeLeadId = null;
					_leadDialog.Close();
					_stateChanged();
					return;
				}

				ShowLeadFailure("That lead is no longer available.");
				break;
			default:
				throw new InvalidOperationException(
					$"Unknown lead dialog choice '{choiceId}'.");
		}
	}

	private void ShowLeadFailure(string line)
	{
		_activeLeadId = null;
		_leadDialog.Open(FacilityNpcDialogs.LeadResponse(_leadSpeakerName, line));
		_stateChanged();
	}

	private EContractCategory CategoryOf(string contractId) =>
		_binding.Map.ContractRegistry.TryGet(contractId, out var contract)
			? contract.Category
			: EContractCategory.Standard;

	private static string Noun(EContractCategory category) =>
		category == EContractCategory.Lead ? "lead" : "contract";

	private void OnClosed()
	{
		_activeLeadId = null;
		_leadSpeakerName = "";
		MapNavigationContext.ClearActiveOperator();
		_stateChanged();
	}
}
