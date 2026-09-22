using Godot;
using GrimSpace.Application;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Presentation.Scene;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public partial class TravelController : Control
{
	private StarSystemOrchestrator _orchestrator = null!;
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
			throw new InvalidOperationException("Travel requires a player execution agent.");

		_activePoiId = MapNavigationContext.ActivePoiId
			?? throw new InvalidOperationException("Travel requires an active POI.");
		_facilityId = MapNavigationContext.ActiveFacilityId
			?? throw new InvalidOperationException("Travel requires an active facility.");
		var poi = _orchestrator.Map.GetPointOfInterest(_activePoiId);
		var facility = poi.GetFacility(_facilityId);

		var scene = GetNode<FacilitySceneView>("Scene");
		FacilityOperatorBinder.Bind(scene, poi, facility, OnFacilityOperatorActivated);

		_backButton = GetNode<Button>("Back");
		_backButton.Pressed += ReturnToMap;
		_npcDialog = new FacilityNpcDialogPresenter(this, _backButton, facility, _orchestrator.Map);
		_deliveryTurnInDialog = new DeliveryTurnInDialogPresenter(
			this,
			_backButton,
			_orchestrator,
			_activePoiId,
			_facilityId);
	}

	public override void _ExitTree()
	{
		_orchestrator.RefreshPlayerAgent();
		base._ExitTree();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_npcDialog.TryHandleInput(@event) || _deliveryTurnInDialog.TryHandleInput(@event))
			return;

		if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
			return;

		if (_npcDialog.IsOpen || _deliveryTurnInDialog.IsOpen)
			return;

		ReturnToMap();
		GetViewport().SetInputAsHandled();
	}

	private void OnFacilityOperatorActivated(FacilityOperator facilityOperator, EFacilityOperatorRole role)
	{
		MapNavigationContext.ActivateOperator(facilityOperator.Name);
		switch (role)
		{
			case EFacilityOperatorRole.Dialog:
				_npcDialog.Open(facilityOperator);
				break;
			case EFacilityOperatorRole.DeliveryTurnIn:
				_deliveryTurnInDialog.Open(facilityOperator);
				break;
			default:
				throw new InvalidOperationException(
					$"Unexpected operator role '{role}' in travel facility.");
		}
	}

	private void ReturnToMap()
	{
		MapNavigationContext.ClearActiveOperator();
		_orchestrator.RefreshPlayerAgent();
		GetTree().ChangeSceneToFile(MapNavigationContext.MapScenePath);
	}
}
