using Godot;
using GrimSpace.Application;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.Components;

namespace GrimSpace.World.StarSystem.Presentation;

public partial class RefineryController : Control
{
	private StarSystemOrchestrator _orchestrator = null!;
	private Button _backButton = null!;

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
		var facility = FacilityLookup.Get(_orchestrator.Map, poiId, facilityId);

		var scene = GetNode<FacilitySceneView>("Scene");
		FacilityOperatorBinder.Bind(scene, facility, OnFacilityOperatorActivated);

		_backButton = GetNode<Button>("Back");
		_backButton.Pressed += ReturnToMap;
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

		ReturnToMap();
		GetViewport().SetInputAsHandled();
	}

	private void OnFacilityOperatorActivated(FacilityOperator facilityOperator)
	{
		MapNavigationContext.ActivateOperator(facilityOperator.Name);
		switch (facilityOperator.Role)
		{
			case EFacilityOperatorRole.Dialog:
				ShowPlaceholderDialog(facilityOperator);
				break;
			default:
				throw new InvalidOperationException(
					$"Unexpected operator role '{facilityOperator.Role}' in refinery facility.");
		}
	}

	private void ShowPlaceholderDialog(FacilityOperator facilityOperator)
	{
		var dialog = new AcceptDialog
		{
			Title = OperatorDisplayLabels.Title(facilityOperator),
			DialogText = "Refining operations are not available yet.",
			Exclusive = true,
		};
		AddChild(dialog);
		dialog.PopupCentered();
		dialog.Confirmed += () => dialog.QueueFree();
		dialog.Canceled += () => dialog.QueueFree();
	}

	private void ReturnToMap()
	{
		MapNavigationContext.ClearActiveOperator();
		_orchestrator.RefreshPlayerAgent();
		GetTree().ChangeSceneToFile(MapNavigationContext.MapScenePath);
	}
}
