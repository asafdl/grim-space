using Godot;
using GrimSpace.Application;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Presentation.Scene;
using GrimSpace.Components;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public partial class DockyardController : Control
{
	private StarSystemOrchestrator _orchestrator = null!;
	private CanvasLayer _dockyardHudLayer = null!;
	private DockyardHudOverlay _dockyardHud = null!;
	private DockyardShieldRechargeHudOverlay _shieldRechargeHud = null!;
	private Button _backButton = null!;
	private FacilityNpcDialogPresenter _npcDialog = null!;

	public override void _Ready()
	{
		_orchestrator = Session.Instance.Run.StarSystem;
		_orchestrator.RefreshPlayerAgent();
		if (_orchestrator.PlayerAgent is null)
			throw new InvalidOperationException("Dockyard requires a player execution agent.");

		var poiId = MapNavigationContext.ActivePoiId
			?? throw new InvalidOperationException("Dockyard requires an active POI.");
		var facilityId = MapNavigationContext.ActiveFacilityId
			?? throw new InvalidOperationException("Dockyard requires an active facility.");
		var facility = _orchestrator.Map.GetPointOfInterest(poiId).GetFacility(facilityId);

		var scene = GetNode<FacilitySceneView>("Scene");
		FacilityOperatorBinder.Bind(scene, facility, OnFacilityOperatorActivated);

		_backButton = GetNode<Button>("Back");
		_backButton.Pressed += ReturnToMap;

		_dockyardHudLayer = new CanvasLayer { Layer = 20 };
		AddChild(_dockyardHudLayer);
		_dockyardHud = new DockyardHudOverlay();
		_dockyardHud.PurchaseRequested += OnPurchaseRequested;
		_dockyardHud.HullRepairRequested += OnHullRepairRequested;
		_dockyardHud.Closed += UpdateBackButton;
		_dockyardHudLayer.AddChild(_dockyardHud);

		_shieldRechargeHud = new DockyardShieldRechargeHudOverlay();
		_shieldRechargeHud.FaceRechargeRequested += OnFaceShieldRechargeRequested;
		_shieldRechargeHud.FillAllRechargeRequested += OnFillAllShieldRechargeRequested;
		_shieldRechargeHud.Closed += UpdateBackButton;
		_dockyardHudLayer.AddChild(_shieldRechargeHud);

		_npcDialog = new FacilityNpcDialogPresenter(this, _backButton, facility, _orchestrator.Map);
	}

	public override void _ExitTree()
	{
		_orchestrator.RefreshPlayerAgent();
		base._ExitTree();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_npcDialog.TryHandleInput(@event))
			return;

		if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
			return;

		if (_dockyardHud.IsOpen || _shieldRechargeHud.IsOpen || _npcDialog.IsOpen)
			return;

		ReturnToMap();
		GetViewport().SetInputAsHandled();
	}

	public bool TryPurchaseUpgrade(string offerId, string shipId)
	{
		var poiId = MapNavigationContext.ActivePoiId
			?? throw new InvalidOperationException("Dockyard requires an active POI.");
		var facilityId = MapNavigationContext.ActiveFacilityId
			?? throw new InvalidOperationException("Dockyard requires an active facility.");
		var operatorName = RequireActiveOperatorName();
		var before = Session.Instance.Run.ShipRegistry.Get(shipId).Clone();
		return _orchestrator.TryCommitPlayerInput(new PurchaseDockyardUpgradeAction(
			State.PlayerFleetUnitId,
			poiId,
			facilityId,
			operatorName,
			offerId,
			before));
	}

	public bool TryPurchaseHullRepair(string shipId)
	{
		var poiId = MapNavigationContext.ActivePoiId
			?? throw new InvalidOperationException("Dockyard requires an active POI.");
		var facilityId = MapNavigationContext.ActiveFacilityId
			?? throw new InvalidOperationException("Dockyard requires an active facility.");
		var operatorName = RequireActiveOperatorName();
		var before = Session.Instance.Run.ShipRegistry.Get(shipId).Clone();
		return _orchestrator.TryCommitPlayerInput(new PurchaseHullRepairAction(
			State.PlayerFleetUnitId,
			poiId,
			facilityId,
			operatorName,
			before));
	}

	public bool TryPurchaseShieldRecharge(string shipId, ESpatialOrientation? face = null)
	{
		var poiId = MapNavigationContext.ActivePoiId
			?? throw new InvalidOperationException("Dockyard requires an active POI.");
		var facilityId = MapNavigationContext.ActiveFacilityId
			?? throw new InvalidOperationException("Dockyard requires an active facility.");
		var operatorName = RequireActiveOperatorName();
		var before = Session.Instance.Run.ShipRegistry.Get(shipId).Clone();
		return _orchestrator.TryCommitPlayerInput(new PurchaseShieldRechargeAction(
			State.PlayerFleetUnitId,
			poiId,
			facilityId,
			operatorName,
			before,
			face));
	}

	private void OnFacilityOperatorActivated(FacilityOperator facilityOperator)
	{
		MapNavigationContext.ActivateOperator(facilityOperator.Name);
		switch (facilityOperator.Role)
		{
			case EFacilityOperatorRole.DockyardShop:
				OpenDockyardHud(facilityOperator);
				break;
			case EFacilityOperatorRole.ShieldRecharge:
				OpenShieldRechargeHud(facilityOperator);
				break;
			case EFacilityOperatorRole.Dialog:
				_npcDialog.Open(facilityOperator);
				break;
			default:
				throw new InvalidOperationException(
					$"Unexpected operator role '{facilityOperator.Role}' in dockyard facility.");
		}
	}

	private void OpenDockyardHud(FacilityOperator facilityOperator)
	{
		_dockyardHud.Open(Session.Instance.Run, _orchestrator.Map, OperatorDisplayLabels.Title(facilityOperator));
		UpdateBackButton();
	}

	private void OpenShieldRechargeHud(FacilityOperator facilityOperator)
	{
		_shieldRechargeHud.Open(Session.Instance.Run, _orchestrator.Map, OperatorDisplayLabels.Title(facilityOperator));
		UpdateBackButton();
	}

	private void OnHullRepairRequested(string shipId)
	{
		if (!TryPurchaseHullRepair(shipId))
		{
			_dockyardHud.ShowError("Unable to repair hull.");
			UpdateBackButton();
			return;
		}

		_dockyardHud.Sync(Session.Instance.Run, _orchestrator.Map);
		_dockyardHud.ShowConfirmation("Hull repaired.", HudStatusKind.Success);
		UpdateBackButton();
	}

	private void OnPurchaseRequested(string offerId, string shipId)
	{
		if (!TryPurchaseUpgrade(offerId, shipId))
		{
			_dockyardHud.ShowError("Unable to purchase upgrade.");
			UpdateBackButton();
			return;
		}

		_dockyardHud.Sync(Session.Instance.Run, _orchestrator.Map);
		_dockyardHud.ShowConfirmation("Upgrade installed.", HudStatusKind.Success);
		UpdateBackButton();
	}

	private void OnFaceShieldRechargeRequested(string shipId, ESpatialOrientation face) =>
		CommitShieldRecharge(shipId, face);

	private void OnFillAllShieldRechargeRequested(string shipId) =>
		CommitShieldRecharge(shipId, null);

	private void CommitShieldRecharge(string shipId, ESpatialOrientation? face)
	{
		if (!TryPurchaseShieldRecharge(shipId, face))
		{
			_shieldRechargeHud.ShowError("Unable to recharge shields.");
			UpdateBackButton();
			return;
		}

		_shieldRechargeHud.Sync(Session.Instance.Run, _orchestrator.Map);
		_shieldRechargeHud.ShowConfirmation("Shields recharged.", HudStatusKind.Success);
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
		?? throw new InvalidOperationException("Dockyard purchase requires an active facility operator.");

	private void UpdateBackButton() =>
		_backButton.Disabled = _dockyardHud.IsOpen || _shieldRechargeHud.IsOpen || _npcDialog.IsOpen;
}
