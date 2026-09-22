using Godot;
using GrimSpace.Application;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Presentation.Scene;
using GrimSpace.Components;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public partial class CommandAuthorityController : Control
{
	private StarSystemOrchestrator _orchestrator = null!;
	private CanvasLayer _contractHudLayer = null!;
	private ContractHudOverlay _contractHud = null!;
	private Button _backButton = null!;
	private FacilityNpcDialogPresenter _npcDialog = null!;
	private string _activePoiId = null!;

	public override void _Ready()
	{
		_orchestrator = Session.Instance.Run.StarSystem;
		_orchestrator.RefreshPlayerAgent();
		if (_orchestrator.PlayerAgent is null)
			throw new InvalidOperationException("Command Authority requires a player execution agent.");

		_activePoiId = MapNavigationContext.ActivePoiId
			?? throw new InvalidOperationException("Command Authority requires an active POI.");
		var facilityId = MapNavigationContext.ActiveFacilityId
			?? throw new InvalidOperationException("Command Authority requires an active facility.");
		var facility = _orchestrator.Map.GetPointOfInterest(_activePoiId).GetFacility(facilityId);

		var scene = GetNode<FacilitySceneView>("Scene");
		FacilityOperatorBinder.Bind(scene, facility, OnFacilityOperatorActivated);

		_backButton = GetNode<Button>("Back");
		_backButton.Pressed += ReturnToMap;

		_contractHudLayer = new CanvasLayer { Layer = 20 };
		AddChild(_contractHudLayer);
		_contractHud = new ContractHudOverlay();
		_contractHud.AcceptRequested += OnAcceptRequested;
		_contractHud.DeclineRequested += OnDeclineRequested;
		_contractHud.Closed += UpdateBackButton;
		_contractHudLayer.AddChild(_contractHud);

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

		if (_contractHud.IsOpen || _npcDialog.IsOpen)
			return;

		ReturnToMap();
		GetViewport().SetInputAsHandled();
	}

	public bool TryAcceptContract(string contractId)
	{
		var facilityId = MapNavigationContext.ActiveFacilityId
			?? throw new InvalidOperationException("Command Authority requires an active facility.");
		var operatorName = RequireActiveOperatorName();
		return _orchestrator.TryCommitPlayerInput(new AcceptContractAction(
			State.PlayerFleetUnitId,
			_activePoiId,
			facilityId,
			operatorName,
			contractId));
	}

	public bool TryDeclineContract(string contractId)
	{
		var facilityId = MapNavigationContext.ActiveFacilityId
			?? throw new InvalidOperationException("Command Authority requires an active facility.");
		var operatorName = RequireActiveOperatorName();
		return _orchestrator.TryCommitPlayerInput(new DeclineContractAction(
			State.PlayerFleetUnitId,
			_activePoiId,
			facilityId,
			operatorName,
			contractId));
	}

	private void OnFacilityOperatorActivated(FacilityOperator facilityOperator)
	{
		MapNavigationContext.ActivateOperator(facilityOperator.Name);
		switch (facilityOperator.Role)
		{
			case EFacilityOperatorRole.Contracts:
				OpenContractHud(facilityOperator);
				break;
			case EFacilityOperatorRole.Dialog:
				_npcDialog.Open(facilityOperator);
				break;
			default:
				throw new InvalidOperationException(
					$"Unexpected operator role '{facilityOperator.Role}' in command authority facility.");
		}
	}

	private void OpenContractHud(FacilityOperator facilityOperator)
	{
		_contractHud.Open(_orchestrator.Map, _activePoiId, OperatorDisplayLabels.Title(facilityOperator));
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

		_contractHud.SyncMap(_orchestrator.Map);
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

		_contractHud.SyncMap(_orchestrator.Map);
		_contractHud.ShowConfirmation("Contract declined.", HudStatusKind.Error);
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
		?? throw new InvalidOperationException("Contract decision requires an active facility operator.");

	private void UpdateBackButton() =>
		_backButton.Disabled = _contractHud.IsOpen || _npcDialog.IsOpen;
}
