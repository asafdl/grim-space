using Godot;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Presentation.Scene;
using GrimSpace.Components;
using GrimSpace.World.StarSystem.Presentation.Ui;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public partial class CommandAuthorityController : Control
{
	private FacilitySceneBinding _binding = null!;
	private CanvasLayer _contractHudLayer = null!;
	private ContractHudOverlay _contractHud = null!;
	private Button _backButton = null!;
	private FacilityNpcDialogPresenter _npcDialog = null!;
	private DeliveryTurnInDialogPresenter _deliveryTurnInDialog = null!;

	public override void _Ready()
	{
		_binding = FacilitySceneBinding.Create("Command Authority");

		var scene = GetNode<FacilitySceneView>("Scene");
		FacilityOperatorBinder.Bind(
			scene,
			_binding.Poi,
			_binding.Facility,
			OnFacilityOperatorActivated);

		_backButton = GetNode<Button>("Back");
		_backButton.Pressed += ReturnToMap;

		_contractHudLayer = new CanvasLayer { Layer = 20 };
		AddChild(_contractHudLayer);
		_contractHud = new ContractHudOverlay();
		_contractHud.AcceptRequested += OnAcceptRequested;
		_contractHud.DeclineRequested += OnDeclineRequested;
		_contractHud.Closed += OnContractHudClosed;
		_contractHudLayer.AddChild(_contractHud);

		_npcDialog = new FacilityNpcDialogPresenter(
			this,
			_backButton,
			_binding.Facility,
			_binding.Map);
		_deliveryTurnInDialog = new DeliveryTurnInDialogPresenter(
			this,
			_backButton,
			_binding.Map,
			_binding.Intents,
			_binding.PoiId,
			_binding.FacilityId);
	}

	public override void _ExitTree()
	{
		_binding.Dispose();
		base._ExitTree();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_npcDialog.TryHandleInput(@event) || _deliveryTurnInDialog.TryHandleInput(@event))
			return;

		if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
			return;

		if (_contractHud.IsOpen || _npcDialog.IsOpen || _deliveryTurnInDialog.IsOpen)
			return;

		GetViewport().SetInputAsHandled();
		ReturnToMap();
	}

	public bool TryAcceptContract(string contractId)
	{
		var operatorName = RequireActiveOperatorName();
		return _binding.Intents.TryAcceptContract(
			_binding.PoiId,
			_binding.FacilityId,
			operatorName,
			contractId);
	}

	public bool TryDeclineContract(string contractId)
	{
		var operatorName = RequireActiveOperatorName();
		return _binding.Intents.TryDeclineContract(
			_binding.PoiId,
			_binding.FacilityId,
			operatorName,
			contractId);
	}

	private void OnFacilityOperatorActivated(FacilityOperator facilityOperator, EFacilityOperatorRole role)
	{
		MapNavigationContext.ActivateOperator(facilityOperator.Name);
		switch (role)
		{
			case EFacilityOperatorRole.Contracts:
				OpenContractHud(facilityOperator);
				break;
			case EFacilityOperatorRole.Dialog:
				_npcDialog.Open(facilityOperator);
				break;
			case EFacilityOperatorRole.DeliveryTurnIn:
				_deliveryTurnInDialog.Open(facilityOperator);
				break;
			default:
				throw new InvalidOperationException(
					$"Unexpected operator role '{role}' in command authority facility.");
		}
	}

	private void OpenContractHud(FacilityOperator facilityOperator)
	{
		if (!_binding.Intents.TryVisitContractMerchant(
			_binding.PoiId,
			_binding.FacilityId,
			facilityOperator.Name))
		{
			GD.PushError(
				$"Unable to record contract merchant visit at POI '{_binding.PoiId}'.");
			return;
		}

		_contractHud.Open(
			_binding.Map,
			_binding.PoiId,
			OperatorDisplayLabels.Title(facilityOperator));
		UpdateBackButton();
	}

	private void OnAcceptRequested(string contractId)
	{
		if (!TryAcceptContract(contractId))
		{
			_contractHud.ShowError("Unable to accept contract.");
			UpdateBackButton();
			return;
		}

		_contractHud.SyncMap(_binding.Map);
		_contractHud.ShowConfirmation("Contract accepted.", HudStatusKind.Success);
		UpdateBackButton();
	}

	private void OnDeclineRequested(string contractId)
	{
		if (!TryDeclineContract(contractId))
		{
			_contractHud.ShowError("Unable to decline contract.");
			UpdateBackButton();
			return;
		}

		_contractHud.SyncMap(_binding.Map);
		_contractHud.ShowConfirmation("Contract declined.", HudStatusKind.Error);
		UpdateBackButton();
	}

	private void ReturnToMap()
	{
		MapNavigationContext.ClearActiveOperator();
		GetTree().ChangeSceneToFile(MapNavigationContext.MapScenePath);
	}

	private static string RequireActiveOperatorName() =>
		MapNavigationContext.ActiveOperatorName
		?? throw new InvalidOperationException("Contract decision requires an active facility operator.");

	private void OnContractHudClosed()
	{
		MapNavigationContext.ClearActiveOperator();
		UpdateBackButton();
	}

	private void UpdateBackButton() =>
		_backButton.Disabled = _contractHud.IsOpen || _npcDialog.IsOpen || _deliveryTurnInDialog.IsOpen;
}
