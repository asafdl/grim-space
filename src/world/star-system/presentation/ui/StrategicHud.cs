using Godot;
using GrimSpace.Application;
using GrimSpace.Components;
using GrimSpace.Core.Actions;
using GrimSpace.Pilots;
using GrimSpace.Presentation.Support;
using GrimSpace.Run;
using GrimSpace.Ui.Components;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Presentation.Scene;
using GrimSpace.World.StarSystem.Objectives;
using GrimSpace.World.StarSystem.Resources;
using FleetTravel = GrimSpace.World.StarSystem.Units.FleetTravel;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

/// <summary>Binds resource feed and objectives sync for <c>strategic_hud.tscn</c>.</summary>
public partial class StrategicHud : CanvasLayer
{
	/// <summary>Above map UI (layer 11) facade pins; below pause menu (layer 15).</summary>
	private const int ModalCanvasLayer = 14;
	private const int PilotSpeechCanvasLayer = 12;

	private PilotSpeechHost _pilotSpeech = null!;
	private CanvasLayer _modalLayer = null!;
	private ObjectivesHud _objectivesHud = null!;
	private ResourceHud _resourceHud = null!;
	private ResourceTransactionFeed _resourceFeed = null!;
	private IDisposable _contractStateSubscription = null!;
	private PlayerFleetHud _playerFleetHud = null!;
	private PilotCharacterSheetOverlay _pilotSheet = null!;
	private SystemErrorBanner _errorBanner = null!;
	private ReportIssueDialog _reportIssueDialog = null!;
	private StarSystemOrchestrator _orchestrator = null!;

	public ObjectivesHud Objectives => _objectivesHud;
	public PlayerFleetHud PlayerFleet => _playerFleetHud;
	public bool IsBlockingModalOpen => _pilotSheet.IsOpen;
	public void PresentInsufficientFunds() => _resourceHud.PresentInsufficientFunds();
	public void PresentSystemError(string summary, string technicalDetails)
	{
		GD.PushError(technicalDetails);
		_errorBanner.ShowError(summary, technicalDetails);
	}

	public override void _Ready()
	{
		_objectivesHud = GetNode<ObjectivesHud>("ObjectivesHud");
		_resourceHud = GetNode<ResourceHud>("ResourceHud");
		_playerFleetHud = GetNode<PlayerFleetHud>("PlayerFleetHud");
		_modalLayer = new CanvasLayer { Layer = ModalCanvasLayer };
		AddChild(_modalLayer);
		_pilotSheet = new PilotCharacterSheetOverlay();
		_modalLayer.AddChild(_pilotSheet);
		_errorBanner = new SystemErrorBanner();
		_errorBanner.ReportRequested += OnErrorReportRequested;
		_modalLayer.AddChild(_errorBanner);
		_reportIssueDialog = new ReportIssueDialog();
		_modalLayer.AddChild(_reportIssueDialog);
		_orchestrator = Session.Instance.Run.StarSystem;
		_resourceFeed = new ResourceTransactionFeed();
		_resourceFeed.Bind(Session.Instance.Run.Transitions, _orchestrator, _resourceHud);
		_contractStateSubscription =
			_orchestrator.Subscribe<Record<ContractStateChanged>>(OnContractStateChanged);
		_objectivesHud.DismissRequested += OnObjectiveDismissRequested;
		_playerFleetHud.PilotSheetRequested += OnPilotSheetRequested;
		_pilotSheet.SkillLearnRequested += OnPilotSkillLearnRequested;
		_pilotSheet.PilotCycleRequested += OnPilotCycleRequested;
		_pilotSheet.DismissRequested += OnPilotDismissRequested;
		SetProcess(true);
		SyncObjectives();
		Session.Instance.Run.RefreshPilotAssignments();
		_pilotSpeech = new PilotSpeechHost(_playerFleetHud.PortraitAnchorFor, PilotSpeechCanvasLayer);
		AddChild(_pilotSpeech);
		_playerFleetHud.BindBarks(Session.Instance.Run.Barks, _pilotSpeech);
		_playerFleetHud.Sync(Session.Instance.Run);
	}

	public override void _Process(double delta)
	{
		SyncObjectives();
		SyncTravelFuelStatus();
		_playerFleetHud.Sync(Session.Instance.Run);
	}

	public override void _ExitTree()
	{
		_objectivesHud.DismissRequested -= OnObjectiveDismissRequested;
		_playerFleetHud.PilotSheetRequested -= OnPilotSheetRequested;
		_pilotSheet.SkillLearnRequested -= OnPilotSkillLearnRequested;
		_pilotSheet.PilotCycleRequested -= OnPilotCycleRequested;
		_pilotSheet.DismissRequested -= OnPilotDismissRequested;
		_errorBanner.ReportRequested -= OnErrorReportRequested;
		_contractStateSubscription.Dispose();
		_resourceFeed.Dispose();
	}

	private void OnContractStateChanged(Record<ContractStateChanged> record)
	{
		var change = record.Value;
		if (!string.Equals(change.HolderUnitId, State.PlayerFleetUnitId, StringComparison.Ordinal))
			return;

		if (change.Status == EContractStatus.Active)
			SyncObjectives();
		_objectivesHud.NotifyContractStateChanged(change.ContractId, change.Status);
		if (change.Status != EContractStatus.Active)
			SyncObjectives();
	}

	private void OnObjectiveDismissRequested(string contractId)
	{
		if (_orchestrator.TryCommitPlayerInput(new DismissContractAction(
			State.PlayerFleetUnitId,
			contractId)))
			return;

		GD.PushWarning($"Unable to dismiss contract '{contractId}'.");
	}

	public bool TryHandleInput(InputEvent @event)
	{
		return _pilotSheet.TryHandleInput(@event);
	}

	private void OnPilotSheetRequested(string pilotId) =>
		TryOpenPilotSheet(pilotId);

	private bool TryOpenPilotSheet(string pilotId)
	{
		var run = Session.Instance.Run;
		if (!run.PilotRegistry.TryGet(pilotId, out var pilot))
		{
			GD.PushWarning($"Unable to open character sheet for unknown pilot '{pilotId}'.");
			return false;
		}

		ShipInstance? assignedShip = null;
		if (pilot.AssignedShipId is not null
			&& run.ItemInventory.TryGet(pilot.AssignedShipId, out var ship))
			assignedShip = ship;
		else if (pilot.AssignedShipId is not null)
			GD.PushWarning(
				$"Character sheet for pilot '{pilotId}': assigned ship '{pilot.AssignedShipId}' was not found.");

		_pilotSheet.Open(
			pilot,
			run.GetFieldedShipIds().Count > 1,
			assignedShip,
			canDismiss: run.PilotRegistry.All.Count > 1);
		return true;
	}

	private void OnPilotDismissRequested(string pilotId)
	{
		var run = Session.Instance.Run;
		if (!run.TryDismissPilot(pilotId))
		{
			GD.PushWarning($"Unable to dismiss pilot '{pilotId}'.");
			return;
		}

		_pilotSheet.Close();
	}

	private void OnPilotCycleRequested(string pilotId, int direction)
	{
		var run = Session.Instance.Run;
		if (!run.PilotRegistry.TryGet(pilotId, out var pilot))
			return;

		var shipIds = run.GetFieldedShipIds();
		var currentIndex = -1;
		for (var index = 0; index < shipIds.Count; index++)
		{
			if (!string.Equals(shipIds[index], pilot.AssignedShipId, StringComparison.Ordinal))
				continue;

			currentIndex = index;
			break;
		}
		if (currentIndex < 0 || shipIds.Count < 2)
			return;

		for (var offset = 1; offset < shipIds.Count; offset++)
		{
			var nextIndex = (currentIndex + direction * offset + shipIds.Count) % shipIds.Count;
			if (!run.PilotRegistry.TryGetForShip(shipIds[nextIndex], out var nextPilot))
				continue;

			TryOpenPilotSheet(nextPilot.Id);
			return;
		}
	}

	private void OnPilotSkillLearnRequested(string pilotId, EPilotSkillId skillId)
	{
		var run = Session.Instance.Run;
		if (!run.TryLearnPilotSkill(pilotId, skillId))
		{
			GD.PushWarning($"Pilot '{pilotId}' cannot learn skill '{skillId}'.");
			return;
		}

		TryOpenPilotSheet(pilotId);
	}

	private void OnErrorReportRequested(string summary, string technicalDetails) =>
		_reportIssueDialog.Open("star-map", summary, technicalDetails);

	private void SyncObjectives()
	{
		var objectives = ObjectivesCollector.Collect(_orchestrator.Map, State.PlayerFleetUnitId);
		_objectivesHud.Sync(objectives);
	}

	private void SyncTravelFuelStatus()
	{
		var map = _orchestrator.Map;
		var playerId = State.PlayerFleetUnitId;
		var runtime = _orchestrator.RuntimeFor(playerId);
		var fuel = map.PlayerResources.GetBalance(ResourceId.Fuel);
		var pendingPath = _orchestrator.PlayerAgent?.PendingCourse?.Path;
		var estimate = TravelFuelEffects.EstimateRemainingFuelUnits(
			map,
			runtime,
			playerId,
			pendingPath);
		var starving = fuel == 0
			&& map.FleetRegistry.TryGet(playerId, out var fleet)
			&& fleet.State.Travel is FleetTravel.Journey;
		_resourceHud.PresentTravelFuelStatus(fuel, estimate, starving);
	}
}
