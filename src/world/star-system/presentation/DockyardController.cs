using Godot;
using GrimSpace.Application;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Agents;
using GrimSpace.Components;

namespace GrimSpace.World.StarSystem.Presentation;

public partial class DockyardController : Control
{
	private StarSystemOrchestrator _orchestrator = null!;
	private StarMapPlayerExecutionAgent _playerAgent = null!;
	private CanvasLayer _dockyardHudLayer = null!;
	private DockyardHudOverlay _dockyardHud = null!;
	private DockyardShieldRechargeHudOverlay _shieldRechargeHud = null!;
	private Button _backButton = null!;

	public override void _Ready()
	{
		_orchestrator = Session.Instance.Run.StarSystem;
		_orchestrator.RefreshPlayerAgent();
		_playerAgent = _orchestrator.PlayerAgent
			?? throw new InvalidOperationException("Dockyard requires a player execution agent.");

		var scene = GetNode<DockyardSceneView>("Scene");
		scene.SalesmanClicked += OpenDockyardHud;
		scene.ShieldRechargeClicked += OpenShieldRechargeHud;

		_backButton = GetNode<Button>("Back");
		_backButton.Pressed += ReturnToMap;

		_dockyardHudLayer = new CanvasLayer { Layer = 20 };
		AddChild(_dockyardHudLayer);
		_dockyardHud = new DockyardHudOverlay();
		_dockyardHud.PurchaseRequested += OnPurchaseRequested;
		_dockyardHud.Closed += UpdateBackButton;
		_dockyardHudLayer.AddChild(_dockyardHud);

		_shieldRechargeHud = new DockyardShieldRechargeHudOverlay();
		_shieldRechargeHud.FaceRechargeRequested += OnFaceShieldRechargeRequested;
		_shieldRechargeHud.FillAllRechargeRequested += OnFillAllShieldRechargeRequested;
		_shieldRechargeHud.Closed += UpdateBackButton;
		_dockyardHudLayer.AddChild(_shieldRechargeHud);
	}

	public override void _ExitTree()
	{
		_orchestrator.RefreshPlayerAgent();
		base._ExitTree();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
			return;

		if (_dockyardHud.IsOpen || _shieldRechargeHud.IsOpen)
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
		var before = Session.Instance.Run.ShipRegistry.Get(shipId).Clone();
		return _orchestrator.TryCommitPlayerInput(new PurchaseDockyardUpgradeAction(
			State.PlayerFleetUnitId,
			poiId,
			facilityId,
			offerId,
			before));
	}

	public bool TryPurchaseShieldRecharge(string shipId, ESpatialOrientation? face = null)
	{
		var poiId = MapNavigationContext.ActivePoiId
			?? throw new InvalidOperationException("Dockyard requires an active POI.");
		var facilityId = MapNavigationContext.ActiveFacilityId
			?? throw new InvalidOperationException("Dockyard requires an active facility.");
		var before = Session.Instance.Run.ShipRegistry.Get(shipId).Clone();
		return _orchestrator.TryCommitPlayerInput(new PurchaseShieldRechargeAction(
			State.PlayerFleetUnitId,
			poiId,
			facilityId,
			before,
			face));
	}

	private void OpenDockyardHud()
	{
		var poiId = MapNavigationContext.ActivePoiId
			?? throw new InvalidOperationException("Dockyard requires an active POI.");
		var facilityId = MapNavigationContext.ActiveFacilityId
			?? throw new InvalidOperationException("Dockyard requires an active facility.");
		var poi = _orchestrator.Map.PointsOfInterest.First(p => p.Id == poiId);
		var facility = poi.Facilities.First(f => f.Id == facilityId);

		_dockyardHud.Open(Session.Instance.Run, _orchestrator.Map, facility.DisplayName);
		UpdateBackButton();
	}

	private void OpenShieldRechargeHud()
	{
		var poiId = MapNavigationContext.ActivePoiId
			?? throw new InvalidOperationException("Dockyard requires an active POI.");
		var facilityId = MapNavigationContext.ActiveFacilityId
			?? throw new InvalidOperationException("Dockyard requires an active facility.");
		var poi = _orchestrator.Map.PointsOfInterest.First(p => p.Id == poiId);
		var facility = poi.Facilities.First(f => f.Id == facilityId);

		_shieldRechargeHud.Open(Session.Instance.Run, _orchestrator.Map, facility.DisplayName);
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
		_orchestrator.RefreshPlayerAgent();
		GetTree().ChangeSceneToFile(MapNavigationContext.MapScenePath);
	}

	private void UpdateBackButton() =>
		_backButton.Disabled = _dockyardHud.IsOpen || _shieldRechargeHud.IsOpen;
}
