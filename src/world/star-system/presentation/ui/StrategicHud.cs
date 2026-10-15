using Godot;
using GrimSpace.Application;
using GrimSpace.Components;
using GrimSpace.Core.Actions;
using GrimSpace.Pilots;
using GrimSpace.Presentation.Support;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Objectives;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

/// <summary>Binds resource feed and objectives sync for <c>strategic_hud.tscn</c>.</summary>
public partial class StrategicHud : CanvasLayer
{
	private ObjectivesHud _objectivesHud = null!;
	private ResourceHud _resourceHud = null!;
	private ResourceTransactionFeed _resourceFeed = null!;
	private IDisposable _contractStateSubscription = null!;
	private PlayerFleetHud _playerFleetHud = null!;
	private FleetManagementOverlay _fleetManagement = null!;
	private PilotCharacterSheetOverlay _pilotSheet = null!;
	private SystemErrorBanner _errorBanner = null!;
	private ReportIssueDialog _reportIssueDialog = null!;
	private StarSystemOrchestrator _orchestrator = null!;
	private string? _fleetReturnShipId;
	private bool _returnToFleetAfterPilotSheet;

	public ObjectivesHud Objectives => _objectivesHud;
	public bool IsBlockingModalOpen => _fleetManagement.IsOpen || _pilotSheet.IsOpen;
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
		_fleetManagement = new FleetManagementOverlay();
		_fleetManagement.PilotSheetRequested += OnFleetPilotSheetRequested;
		_fleetManagement.PilotAssignmentRequested += OnPilotAssignmentRequested;
		AddChild(_fleetManagement);
		_pilotSheet = new PilotCharacterSheetOverlay();
		_pilotSheet.Closed += OnPilotSheetClosed;
		_pilotSheet.FleetManagementRequested += OnPilotFleetManagementRequested;
		AddChild(_pilotSheet);
		_errorBanner = new SystemErrorBanner();
		_errorBanner.ReportRequested += OnErrorReportRequested;
		AddChild(_errorBanner);
		_reportIssueDialog = new ReportIssueDialog();
		AddChild(_reportIssueDialog);
		_orchestrator = Session.Instance.Run.StarSystem;
		_resourceFeed = new ResourceTransactionFeed();
		_resourceFeed.Bind(Session.Instance.Run.Transitions, _orchestrator, _resourceHud);
		_contractStateSubscription =
			_orchestrator.Subscribe<Record<ContractStateChanged>>(OnContractStateChanged);
		_objectivesHud.DismissRequested += OnObjectiveDismissRequested;
		_playerFleetHud.FleetManagementRequested += OnFleetManagementRequested;
		_playerFleetHud.PilotSheetRequested += OnPilotSheetRequested;
		_pilotSheet.SkillLearnRequested += OnPilotSkillLearnRequested;
		_pilotSheet.PilotCycleRequested += OnPilotCycleRequested;
		SetProcess(true);
		SyncObjectives();
		_playerFleetHud.Sync(Session.Instance.Run, _orchestrator);
	}

	public override void _Process(double _)
	{
		SyncObjectives();
		_playerFleetHud.Sync(Session.Instance.Run, _orchestrator);
	}

	public override void _ExitTree()
	{
		_objectivesHud.DismissRequested -= OnObjectiveDismissRequested;
		_playerFleetHud.FleetManagementRequested -= OnFleetManagementRequested;
		_playerFleetHud.PilotSheetRequested -= OnPilotSheetRequested;
		_fleetManagement.PilotSheetRequested -= OnFleetPilotSheetRequested;
		_fleetManagement.PilotAssignmentRequested -= OnPilotAssignmentRequested;
		_pilotSheet.Closed -= OnPilotSheetClosed;
		_pilotSheet.FleetManagementRequested -= OnPilotFleetManagementRequested;
		_pilotSheet.SkillLearnRequested -= OnPilotSkillLearnRequested;
		_pilotSheet.PilotCycleRequested -= OnPilotCycleRequested;
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
		if (@event.IsActionPressed("map_fleet_management", false, true))
		{
			if (_pilotSheet.IsOpen)
				return false;

			if (_fleetManagement.IsOpen)
				_fleetManagement.Close();
			else
				OnFleetManagementRequested();
			return true;
		}

		return _pilotSheet.TryHandleInput(@event) || _fleetManagement.TryHandleInput(@event);
	}

	private void OnFleetManagementRequested()
	{
		_returnToFleetAfterPilotSheet = false;
		var run = Session.Instance.Run;
		var selectedShipId = PlayerFleetSelection.EnsureSelectedMember(
			_orchestrator.Map,
			_orchestrator.RuntimeFor(State.PlayerFleetUnitId),
			State.PlayerFleetUnitId,
			run.GetFieldedShipIds());
		_fleetManagement.Open(run, selectedShipId);
	}

	private void OnPilotSheetRequested(string pilotId)
	{
		_returnToFleetAfterPilotSheet = false;
		TryOpenPilotSheet(pilotId);
	}

	private void OnFleetPilotSheetRequested(string pilotId)
	{
		_fleetReturnShipId = _fleetManagement.SelectedShipId;
		_returnToFleetAfterPilotSheet = true;
		_fleetManagement.Close();
		if (TryOpenPilotSheet(pilotId))
			return;

		_returnToFleetAfterPilotSheet = false;
		_fleetManagement.Open(Session.Instance.Run, _fleetReturnShipId);
	}

	private void OnPilotSheetClosed()
	{
		if (!_returnToFleetAfterPilotSheet)
			return;

		_returnToFleetAfterPilotSheet = false;
		_fleetManagement.Open(Session.Instance.Run, _fleetReturnShipId);
	}

	private void OnPilotFleetManagementRequested(string pilotId)
	{
		var run = Session.Instance.Run;
		var shipId = run.PilotRegistry.TryGet(pilotId, out var pilot)
			&& pilot.AssignedShipId is not null
			&& run.ItemInventory.IsPlayerOwned(pilot.AssignedShipId)
				? pilot.AssignedShipId
				: null;
		_returnToFleetAfterPilotSheet = false;
		_pilotSheet.Close();
		_fleetManagement.Open(run, shipId);
	}

	private void OnPilotAssignmentRequested(string pilotId, string shipId)
	{
		if (Session.Instance.Run.TryAssignPilotToShip(pilotId, shipId))
			return;

		GD.PushWarning($"Unable to assign pilot '{pilotId}' to ship '{shipId}'.");
	}

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

		_pilotSheet.Open(pilot, run.GetFieldedShipIds().Count > 1, assignedShip);
		return true;
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
}
