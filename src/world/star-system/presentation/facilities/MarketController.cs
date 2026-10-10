using Godot;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Presentation.Scene;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public partial class MarketController : Control
{
	private FacilitySceneBinding _binding = null!;
	private CanvasLayer _merchantHudLayer = null!;
	private ShipRecruitmentHudOverlay _shipRecruitmentHud = null!;
	private FacilityContractHudPresenter _contractHud = null!;
	private Button _backButton = null!;
	private FacilityNpcDialogPresenter _npcDialog = null!;
	private DeliveryTurnInDialogPresenter _deliveryTurnInDialog = null!;

	public override void _Ready()
	{
		_binding = FacilitySceneBinding.Create("Market");

		var scene = GetNode<FacilitySceneView>("Scene");

		_backButton = GetNode<Button>("Back");
		_backButton.Pressed += ReturnToMap;

		_merchantHudLayer = new CanvasLayer { Layer = 5 };
		AddChild(_merchantHudLayer);
		_shipRecruitmentHud = new ShipRecruitmentHudOverlay();
		_shipRecruitmentHud.RecruitmentRequested += OnRecruitmentRequested;
		_shipRecruitmentHud.Closed += UpdateBackButton;
		_merchantHudLayer.AddChild(_shipRecruitmentHud);
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

		if (_shipRecruitmentHud.IsOpen
			|| _contractHud.IsOpen
			|| _npcDialog.IsOpen
			|| _deliveryTurnInDialog.IsOpen)
			return;

		GetViewport().SetInputAsHandled();
		ReturnToMap();
	}

	private void OnFacilityOperatorActivated(FacilityOperator facilityOperator, EFacilityOperatorRole role)
	{
		MapNavigationContext.ActivateOperator(facilityOperator.Name);
		switch (role)
		{
			case EFacilityOperatorRole.Merchant when facilityOperator.MerchantCatalog == EMerchantCatalog.Ships:
				_shipRecruitmentHud.Open(
					_binding.Run,
					OperatorDisplayLabels.Title(facilityOperator));
				UpdateBackButton();
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
					$"Unexpected operator role '{role}' in market facility.");
		}
	}

	private void OnWorldUpdated()
	{
		_shipRecruitmentHud.Sync(_binding.Run);
		_contractHud.SyncMap();
	}

	private void OnRecruitmentRequested(ShipRecruitmentCatalog.Offer offer)
	{
		var committed = _binding.Intents.TryRecruitShip(
			_binding.PoiId,
			_binding.FacilityId,
			RequireActiveOperatorName(),
			offer);
		if (!committed)
		{
			var run = _binding.Run;
			if (run.PlayerParty.ShipIds.Count >= EnlistPlayerShipActionDef.MaxPlayerShips)
				_shipRecruitmentHud.ShowError("Your fleet roster is full.");
			else if (!_binding.Map.PlayerResources.CanApply(offer.Cost.Negate()))
				_shipRecruitmentHud.ShowError("You do not have the required resources.");
			else
				_shipRecruitmentHud.ShowError("Unable to hire this ship.");
			UpdateBackButton();
			return;
		}

		_shipRecruitmentHud.ShowConfirmation("Ship hired.");
		UpdateBackButton();
	}

	private void ReturnToMap()
	{
		MapNavigationContext.ClearActiveOperator();
		GetTree().ChangeSceneToFile(MapNavigationContext.MapScenePath);
	}

	private static string RequireActiveOperatorName() =>
		MapNavigationContext.ActiveOperatorName
		?? throw new InvalidOperationException("Ship recruitment requires an active facility operator.");

	private void UpdateBackButton() =>
		_backButton.Disabled = _shipRecruitmentHud.IsOpen
			|| _contractHud.IsOpen
			|| _npcDialog.IsOpen
			|| _deliveryTurnInDialog.IsOpen;
}
