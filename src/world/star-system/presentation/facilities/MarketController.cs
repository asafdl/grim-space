using Godot;
using GrimSpace.Application;
using GrimSpace.Core.Ids;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Presentation.Scene;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public partial class MarketController : Control
{
	private StarSystemOrchestrator _orchestrator = null!;
	private CanvasLayer _merchantHudLayer = null!;
	private ShipRecruitmentHudOverlay _shipRecruitmentHud = null!;
	private Button _backButton = null!;
	private FacilityNpcDialogPresenter _npcDialog = null!;
	private DeliveryTurnInDialogPresenter _deliveryTurnInDialog = null!;
	private string _activePoiId = null!;
	private string _facilityId = null!;

	public override void _Ready()
	{
		_orchestrator = Session.Instance.Run.StarSystem;
		_orchestrator.RefreshPlayerAgent();
		if (_orchestrator.PlayerAgent is null)
			throw new InvalidOperationException("Market requires a player execution agent.");

		_activePoiId = MapNavigationContext.ActivePoiId
			?? throw new InvalidOperationException("Market requires an active POI.");
		_facilityId = MapNavigationContext.ActiveFacilityId
			?? throw new InvalidOperationException("Market requires an active facility.");
		var poi = _orchestrator.Map.GetPointOfInterest(_activePoiId);
		var facility = poi.GetFacility(_facilityId);

		var scene = GetNode<FacilitySceneView>("Scene");

		_backButton = GetNode<Button>("Back");
		_backButton.Pressed += ReturnToMap;

		_merchantHudLayer = new CanvasLayer { Layer = 5 };
		AddChild(_merchantHudLayer);
		_shipRecruitmentHud = new ShipRecruitmentHudOverlay();
		_shipRecruitmentHud.RecruitmentRequested += OnRecruitmentRequested;
		_shipRecruitmentHud.Closed += UpdateBackButton;
		_merchantHudLayer.AddChild(_shipRecruitmentHud);

		_npcDialog = new FacilityNpcDialogPresenter(this, _backButton, facility, _orchestrator.Map);
		_deliveryTurnInDialog = new DeliveryTurnInDialogPresenter(
			this,
			_backButton,
			_orchestrator,
			_activePoiId,
			_facilityId);
		_orchestrator.WorldUpdated += OnWorldUpdated;
		FacilityOperatorBinder.Bind(scene, poi, facility, OnFacilityOperatorActivated);
	}

	public override void _ExitTree()
	{
		_orchestrator.WorldUpdated -= OnWorldUpdated;
		_orchestrator.RefreshPlayerAgent();
		base._ExitTree();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_npcDialog.TryHandleInput(@event) || _deliveryTurnInDialog.TryHandleInput(@event))
			return;

		if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
			return;

		if (_shipRecruitmentHud.IsOpen || _npcDialog.IsOpen || _deliveryTurnInDialog.IsOpen)
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
				_shipRecruitmentHud.Open(Session.Instance.Run, OperatorDisplayLabels.Title(facilityOperator));
				UpdateBackButton();
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

	private void OnWorldUpdated() =>
		_shipRecruitmentHud.Sync(Session.Instance.Run);

	private void OnRecruitmentRequested(ShipRecruitmentCatalog.Offer offer)
	{
		var declaration = new ShipSpawnDeclaration(
			TypedIdGenerator.NextId(UnitTypeSlug.For(offer.Chassis)),
			offer.Chassis,
			offer.GearTier);
		var committed = _orchestrator.TryCommitPlayerInput(new RecruitShipAction(
			State.PlayerFleetUnitId,
			_activePoiId,
			_facilityId,
			RequireActiveOperatorName(),
			declaration));
		if (!committed)
		{
			var run = Session.Instance.Run;
			if (run.PlayerParty.ShipIds.Count >= EnlistPlayerShipActionDef.MaxPlayerShips)
				_shipRecruitmentHud.ShowError("Your fleet roster is full.");
			else if (!run.StarSystem.Map.PlayerResources.CanApply(offer.Cost.Negate()))
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
		_orchestrator.RefreshPlayerAgent();
		GetTree().ChangeSceneToFile(MapNavigationContext.MapScenePath);
	}

	private static string RequireActiveOperatorName() =>
		MapNavigationContext.ActiveOperatorName
		?? throw new InvalidOperationException("Ship recruitment requires an active facility operator.");

	private void UpdateBackButton() =>
		_backButton.Disabled = _shipRecruitmentHud.IsOpen
			|| _npcDialog.IsOpen
			|| _deliveryTurnInDialog.IsOpen;
}
