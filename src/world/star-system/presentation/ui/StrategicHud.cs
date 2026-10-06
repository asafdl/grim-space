using Godot;
using GrimSpace.Application;
using GrimSpace.Core.Actions;
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
	private StarSystemOrchestrator _orchestrator = null!;

	public ObjectivesHud Objectives => _objectivesHud;

	public override void _Ready()
	{
		_objectivesHud = GetNode<ObjectivesHud>("ObjectivesHud");
		_resourceHud = GetNode<ResourceHud>("ResourceHud");
		_playerFleetHud = GetNode<PlayerFleetHud>("PlayerFleetHud");
		_orchestrator = Session.Instance.Run.StarSystem;
		_resourceFeed = new ResourceTransactionFeed();
		_resourceFeed.Bind(Session.Instance.Run.Transitions, _orchestrator, _resourceHud);
		_contractStateSubscription =
			_orchestrator.Subscribe<Record<ContractStateChanged>>(OnContractStateChanged);
		_objectivesHud.DismissRequested += OnObjectiveDismissRequested;
		SetProcess(true);
		SyncObjectives();
		_playerFleetHud.Sync(Session.Instance.Run);
	}

	public override void _Process(double _)
	{
		SyncObjectives();
		_playerFleetHud.Sync(Session.Instance.Run);
	}

	public override void _ExitTree()
	{
		_objectivesHud.DismissRequested -= OnObjectiveDismissRequested;
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

	private void SyncObjectives()
	{
		var objectives = ObjectivesCollector.Collect(_orchestrator.Map, State.PlayerFleetUnitId);
		_objectivesHud.Sync(objectives);
	}
}
