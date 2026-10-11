using Godot;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Presentation.Scene;
using GrimSpace.Components;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public partial class DockyardController : Control
{
	private FacilitySceneBinding _binding = null!;
	private CanvasLayer _dockyardHudLayer = null!;
	private DockyardHudOverlay _dockyardHud = null!;
	private HealingStationPresenter _healingStation = null!;
	private FacilityContractHudPresenter _contractHud = null!;
	private Button _backButton = null!;
	private FacilityNpcDialogPresenter _npcDialog = null!;
	private DeliveryTurnInDialogPresenter _deliveryTurnInDialog = null!;

	public override void _Ready()
	{
		_binding = FacilitySceneBinding.Create("Dockyard");

		var scene = GetNode<FacilitySceneView>("Scene");

		_backButton = GetNode<Button>("Back");
		_backButton.Pressed += ReturnToMap;

		_dockyardHudLayer = new CanvasLayer { Layer = 5 };
		AddChild(_dockyardHudLayer);
		_dockyardHud = new DockyardHudOverlay();
		_dockyardHud.PurchaseRequested += OnWeaponsPurchaseRequested;
		_dockyardHud.Closed += UpdateBackButton;
		_dockyardHudLayer.AddChild(_dockyardHud);

		_healingStation = new HealingStationPresenter(
			this,
			_binding,
			UpdateBackButton);
		_contractHud = new FacilityContractHudPresenter(this, _binding, UpdateBackButton);

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
		_binding.WorldUpdated += OnWorldUpdated;
		FacilityOperatorBinder.Bind(
			scene,
			_binding.Poi,
			_binding.Facility,
			OnFacilityOperatorActivated);
	}

	public override void _ExitTree()
	{
		_binding.WorldUpdated -= OnWorldUpdated;
		_binding.Dispose();
		base._ExitTree();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_npcDialog.TryHandleInput(@event) || _deliveryTurnInDialog.TryHandleInput(@event))
			return;

		if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
			return;

		if (_dockyardHud.IsOpen
			|| _healingStation.IsOpen
			|| _contractHud.IsOpen
			|| _npcDialog.IsOpen
			|| _deliveryTurnInDialog.IsOpen)
			return;

		GetViewport().SetInputAsHandled();
		ReturnToMap();
	}

	public bool TryPurchaseMerchantChange(
		EMerchantCatalog catalog,
		MerchantCatalog.Offering offering,
		MerchantCatalog.Target target,
		string shipId)
	{
		var operatorName = RequireActiveOperatorName();
		var before = _binding.Run.ShipRegistry.Get(shipId).Clone();
		return _binding.Intents.TryPurchase(
			_binding.PoiId,
			_binding.FacilityId,
			operatorName,
			catalog,
			offering,
			target,
			before);
	}

	private void OnFacilityOperatorActivated(FacilityOperator facilityOperator, EFacilityOperatorRole role)
	{
		MapNavigationContext.ActivateOperator(facilityOperator.Name);
		switch (role)
		{
			case EFacilityOperatorRole.Merchant when facilityOperator.MerchantCatalog == EMerchantCatalog.Weapons:
				OpenDockyardHud(facilityOperator);
				break;
			case EFacilityOperatorRole.Merchant when facilityOperator.MerchantCatalog == EMerchantCatalog.ShipSupport:
				OpenDockyardHud(facilityOperator);
				break;
			case EFacilityOperatorRole.HealingStation:
				_healingStation.Open(facilityOperator);
				break;
			case EFacilityOperatorRole.Contracts:
			case EFacilityOperatorRole.StoryContact:
				_contractHud.Open(facilityOperator);
				break;
			case EFacilityOperatorRole.Dialog:
				_npcDialog.Open(facilityOperator);
				break;
			case EFacilityOperatorRole.DeliveryTurnIn:
				_deliveryTurnInDialog.Open(facilityOperator);
				break;
			default:
				throw new InvalidOperationException(
					$"Unexpected operator role '{role}' in dockyard facility.");
		}
	}

	private void OnWorldUpdated()
	{
		_dockyardHud.Sync(_binding.Run, _binding.Map);
		_contractHud.SyncMap();
	}

	private void OpenDockyardHud(FacilityOperator facilityOperator)
	{
		_dockyardHud.Open(
			_binding.Run,
			_binding.Map,
			OperatorDisplayLabels.Title(facilityOperator),
			facilityOperator.MerchantCatalog
			?? throw new InvalidOperationException(
				$"Merchant '{facilityOperator.Name}' requires a catalog."));
		UpdateBackButton();
	}

	private void OnWeaponsPurchaseRequested(
		EMerchantCatalog catalog,
		MerchantCatalog.Offering offering,
		MerchantCatalog.Target target,
		string shipId)
	{
		if (!TryPurchaseMerchantChange(catalog, offering, target, shipId))
		{
			_dockyardHud.ShowError("Unable to purchase upgrade.");
			UpdateBackButton();
			return;
		}

		_dockyardHud.Sync(_binding.Run, _binding.Map);
		var message = offering.Kind == MerchantCatalog.Kind.UpgradeMaxShields
			? "Max shields upgraded."
			: "Upgrade installed.";
		_dockyardHud.ShowConfirmation(message, HudStatusKind.Success);
		UpdateBackButton();
	}

	private void ReturnToMap()
	{
		MapNavigationContext.ClearActiveOperator();
		GetTree().ChangeSceneToFile(MapNavigationContext.MapScenePath);
	}

	private static string RequireActiveOperatorName() =>
		MapNavigationContext.ActiveOperatorName
		?? throw new InvalidOperationException("Dockyard purchase requires an active facility operator.");

	private void UpdateBackButton() =>
		_backButton.Disabled = _dockyardHud.IsOpen
			|| _healingStation.IsOpen
			|| _contractHud.IsOpen
			|| _npcDialog.IsOpen
			|| _deliveryTurnInDialog.IsOpen;
}
