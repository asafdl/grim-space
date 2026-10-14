using Godot;
using GrimSpace.Application;
using GrimSpace.Core.Actions;
using GrimSpace.Pilots;
using GrimSpace.Run;
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
	private PilotCharacterSheetOverlay _pilotSheet = null!;
	private StarSystemOrchestrator _orchestrator = null!;

	public ObjectivesHud Objectives => _objectivesHud;
	public bool IsBlockingModalOpen => _pilotSheet.IsOpen;

	public override void _Ready()
	{
		_objectivesHud = GetNode<ObjectivesHud>("ObjectivesHud");
		_resourceHud = GetNode<ResourceHud>("ResourceHud");
		_playerFleetHud = GetNode<PlayerFleetHud>("PlayerFleetHud");
		_pilotSheet = new PilotCharacterSheetOverlay();
		AddChild(_pilotSheet);
		_orchestrator = Session.Instance.Run.StarSystem;
		_resourceFeed = new ResourceTransactionFeed();
		_resourceFeed.Bind(Session.Instance.Run.Transitions, _orchestrator, _resourceHud);
		_contractStateSubscription =
			_orchestrator.Subscribe<Record<ContractStateChanged>>(OnContractStateChanged);
		_objectivesHud.DismissRequested += OnObjectiveDismissRequested;
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
		_playerFleetHud.PilotSheetRequested -= OnPilotSheetRequested;
		_pilotSheet.SkillLearnRequested -= OnPilotSkillLearnRequested;
		_pilotSheet.PilotCycleRequested -= OnPilotCycleRequested;
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

	public bool TryHandleInput(InputEvent @event) => _pilotSheet.TryHandleInput(@event);

	private void OnPilotSheetRequested(string pilotId) => TryOpenPilotSheet(pilotId);

	private void TryOpenPilotSheet(string pilotId)
	{
		var run = Session.Instance.Run;
		if (!run.PilotRegistry.TryGet(pilotId, out var pilot))
		{
			GD.PushWarning($"Unable to open character sheet for unknown pilot '{pilotId}'.");
			return;
		}

		if (!run.ShipRegistry.TryGet(pilot.AssignedShipId, out var ship))
		{
			GD.PushWarning(
				$"Unable to open character sheet for pilot '{pilotId}': "
				+ $"assigned ship '{pilot.AssignedShipId}' was not found.");
			return;
		}

		_pilotSheet.Open(pilot, ship, run.PlayerParty.ShipIds.Count > 1);
	}

	private void OnPilotCycleRequested(string pilotId, int direction)
	{
		var run = Session.Instance.Run;
		if (!run.PilotRegistry.TryGet(pilotId, out var pilot))
			return;

		var shipIds = run.PlayerParty.ShipIds;
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

	private void SyncObjectives()
	{
		var objectives = ObjectivesCollector.Collect(_orchestrator.Map, State.PlayerFleetUnitId);
		_objectivesHud.Sync(objectives);
	}
}
