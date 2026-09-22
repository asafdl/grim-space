using Godot;
using GrimSpace.Application;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Presentation.Scene;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public partial class RefineryController : Control
{
	private StarSystemOrchestrator _orchestrator = null!;
	private Button _backButton = null!;
	private FacilityNpcDialogPresenter _npcDialog = null!;

	public override void _Ready()
	{
		_orchestrator = Session.Instance.Run.StarSystem;
		_orchestrator.RefreshPlayerAgent();
		if (_orchestrator.PlayerAgent is null)
			throw new InvalidOperationException("Refinery requires a player execution agent.");

		var poiId = MapNavigationContext.ActivePoiId
			?? throw new InvalidOperationException("Refinery requires an active POI.");
		var facilityId = MapNavigationContext.ActiveFacilityId
			?? throw new InvalidOperationException("Refinery requires an active facility.");
		var facility = _orchestrator.Map.GetPointOfInterest(poiId).GetFacility(facilityId);

		var scene = GetNode<FacilitySceneView>("Scene");
		FacilityOperatorBinder.Bind(scene, facility, OnFacilityOperatorActivated);

		_backButton = GetNode<Button>("Back");
		_backButton.Pressed += ReturnToMap;
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

		if (_npcDialog.IsOpen)
			return;

		ReturnToMap();
		GetViewport().SetInputAsHandled();
	}

	private void OnFacilityOperatorActivated(FacilityOperator facilityOperator)
	{
		MapNavigationContext.ActivateOperator(facilityOperator.Name);
		switch (facilityOperator.Role)
		{
			case EFacilityOperatorRole.Dialog:
				_npcDialog.Open(facilityOperator);
				break;
			default:
				throw new InvalidOperationException(
					$"Unexpected operator role '{facilityOperator.Role}' in refinery facility.");
		}
	}

	private void ReturnToMap()
	{
		MapNavigationContext.ClearActiveOperator();
		_orchestrator.RefreshPlayerAgent();
		GetTree().ChangeSceneToFile(MapNavigationContext.MapScenePath);
	}
}
