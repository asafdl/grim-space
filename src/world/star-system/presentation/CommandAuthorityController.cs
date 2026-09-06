using Godot;
using GrimSpace.Core;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Agents;
using GrimSpace.Presentation.Ui.Hud;

namespace GrimSpace.World.StarSystem.Presentation;

public partial class CommandAuthorityController : Control
{
	private StarSystemOrchestrator _orchestrator = null!;
	private StarMapPlayerExecutionAgent _playerAgent = null!;
	private ContractHudOverlay _contractHud = null!;
	private Button _backButton = null!;

	public override void _Ready()
	{
		_orchestrator = RunSession.Instance.Run.StarSystem;
		_playerAgent = _orchestrator.PlayerAgent
			?? throw new InvalidOperationException("Command Authority requires a player execution agent.");
		_orchestrator.EnterInteractive();

		var scene = GetNode<CommandAuthoritySceneView>("Scene");
		scene.GiverClicked += OpenContractHud;

		_backButton = GetNode<Button>("Back");
		_backButton.Pressed += ReturnToMap;

		_contractHud = new ContractHudOverlay();
		_contractHud.AcceptRequested += OnAcceptRequested;
		_contractHud.DeclineRequested += OnDeclineRequested;
		_contractHud.Closed += UpdateBackButton;
		AddChild(_contractHud);
	}

	public override void _ExitTree()
	{
		_orchestrator.ExitInteractive();
		base._ExitTree();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
			return;

		if (_contractHud.IsOpen)
			return;

		ReturnToMap();
		GetViewport().SetInputAsHandled();
	}

	public bool TryAcceptContract(string contractId) =>
		_playerAgent.TryEnqueue([new AcceptContractAction(State.PlayerFleetUnitId, contractId)]);

	public bool TryDeclineContract(string contractId) =>
		_playerAgent.TryEnqueue([new DeclineContractAction(State.PlayerFleetUnitId, contractId)]);

	private void OpenContractHud()
	{
		var poiId = MapNavigationContext.ActivePoiId
			?? throw new InvalidOperationException("Command Authority requires an active POI.");
		var facilityId = MapNavigationContext.ActiveFacilityId
			?? throw new InvalidOperationException("Command Authority requires an active facility.");
		var poi = _orchestrator.Map.PointsOfInterest.First(p => p.Id == poiId);
		var facility = poi.Facilities.First(f => f.Id == facilityId);

		_contractHud.Open(_orchestrator.Map, poiId, facility.DisplayName);
		UpdateBackButton();
	}

	private void OnAcceptRequested(string contractId)
	{
		if (!TryAcceptContract(contractId))
			return;

		_contractHud.ShowConfirmation("Contract accepted.", HudStatusKind.Success);
		UpdateBackButton();
	}

	private void OnDeclineRequested(string contractId)
	{
		if (!TryDeclineContract(contractId))
			return;

		_contractHud.ShowConfirmation("Contract declined.", HudStatusKind.Error);
		UpdateBackButton();
	}

	private void ReturnToMap() =>
		GetTree().ChangeSceneToFile(MapNavigationContext.MapScenePath);

	private void UpdateBackButton() =>
		_backButton.Disabled = _contractHud.IsOpen;
}
