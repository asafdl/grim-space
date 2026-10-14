using Godot;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Presentation.Scene;
using GrimSpace.World.StarSystem.Presentation.Ui;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public partial class TravelController : Control
{
	private FacilitySceneBinding _binding = null!;
	private Button _backButton = null!;
	private FacilityContractHudPresenter _contractHud = null!;
	private MerchantPresenter _merchant = null!;
	private HealingStationPresenter _healingStation = null!;
	private FacilityNpcDialogPresenter _npcDialog = null!;
	private DeliveryTurnInDialogPresenter _deliveryTurnInDialog = null!;

	public override void _Ready()
	{
		_binding = FacilitySceneBinding.Create(
			"Travel",
			GetNode<StrategicHud>("StrategicHud").PresentInsufficientFunds,
			GetNode<StrategicHud>("StrategicHud").PresentSystemError);

		var scene = GetNode<FacilitySceneView>("Scene");
		FacilityOperatorBinder.Bind(
			scene,
			_binding.Poi,
			_binding.Facility,
			OnFacilityOperatorActivated);

		_backButton = GetNode<Button>("Back");
		_backButton.Pressed += ReturnToMap;
		_contractHud = new FacilityContractHudPresenter(this, _binding, UpdateBackButton);
		_merchant = new MerchantPresenter(this, _binding, UpdateBackButton);
		_healingStation = new HealingStationPresenter(
			this,
			_binding,
			UpdateBackButton);
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
		if (_contractHud.TryHandleInput(@event)
			|| _npcDialog.TryHandleInput(@event)
			|| _deliveryTurnInDialog.TryHandleInput(@event))
			return;

		if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
			return;

		if (_merchant.IsOpen
			|| _healingStation.IsOpen
			|| _contractHud.IsOpen
			|| _npcDialog.IsOpen
			|| _deliveryTurnInDialog.IsOpen)
			return;

		GetViewport().SetInputAsHandled();
		ReturnToMap();
	}

	private void OnFacilityOperatorActivated(
		FacilityOperator facilityOperator,
		OperatorInteraction interaction)
	{
		MapNavigationContext.ActivateOperator(facilityOperator.Name);
		switch (interaction.Role)
		{
			case EFacilityOperatorRole.Merchant:
				_merchant.Open(facilityOperator, interaction);
				break;
			case EFacilityOperatorRole.HealingStation:
				_healingStation.Open(facilityOperator);
				break;
			case EFacilityOperatorRole.Contracts:
			case EFacilityOperatorRole.StoryContact:
			case EFacilityOperatorRole.LeadContact:
				_contractHud.Open(facilityOperator, interaction);
				break;
			case EFacilityOperatorRole.Dialog:
				_npcDialog.Open(facilityOperator);
				break;
			case EFacilityOperatorRole.DeliveryTurnIn:
				_deliveryTurnInDialog.Open(facilityOperator);
				break;
			default:
				throw new InvalidOperationException(
					$"Unexpected operator role '{interaction.Role}' in travel facility.");
		}
	}

	private void ReturnToMap()
	{
		MapNavigationContext.ClearActiveOperator();
		GetTree().ChangeSceneToFile(MapNavigationContext.MapScenePath);
	}

	private void UpdateBackButton() =>
		_backButton.Disabled = _merchant.IsOpen
			|| _healingStation.IsOpen
			|| _contractHud.IsOpen
			|| _npcDialog.IsOpen
			|| _deliveryTurnInDialog.IsOpen;
}
