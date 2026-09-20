using Godot;
using GrimSpace.Application;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.Objectives;

namespace GrimSpace.World.StarSystem.Presentation;

/// <summary>Binds resource feed and objectives sync for <c>strategic_hud.tscn</c>.</summary>
public partial class StrategicHud : CanvasLayer
{
	private ObjectivesHud _objectivesHud = null!;
	private ResourceHud _resourceHud = null!;
	private ResourceTransactionFeed _resourceFeed = null!;
	private StarSystemOrchestrator _orchestrator = null!;

	public ObjectivesHud Objectives => _objectivesHud;

	public override void _Ready()
	{
		_objectivesHud = GetNode<ObjectivesHud>("ObjectivesHud");
		_resourceHud = GetNode<ResourceHud>("ResourceHud");
		_orchestrator = Session.Instance.Run.StarSystem;
		_resourceFeed = new ResourceTransactionFeed();
		_resourceFeed.Bind(Session.Instance.Run.Transitions, _orchestrator, _resourceHud);
		SetProcess(true);
		SyncObjectives();
	}

	public override void _Process(double _) => SyncObjectives();

	public override void _ExitTree() => _resourceFeed.Dispose();

	private void SyncObjectives()
	{
		var objectives = ObjectivesCollector.Collect(_orchestrator.Map, State.PlayerFleetUnitId);
		_objectivesHud.Sync(objectives);
	}
}
