using Godot;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public partial class PlayerFleetHud : MarginContainer
{
	private readonly Dictionary<string, ShipPilot> _pilots = new(StringComparer.Ordinal);
	private VBoxContainer _pilotColumn = null!;
	private StarSystemOrchestrator? _orchestrator;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;

		var margin = new MarginContainer();
		foreach (var marginName in new[] { "margin_left", "margin_top", "margin_right", "margin_bottom" })
			margin.AddThemeConstantOverride(marginName, 8);
		AddChild(margin);

		_pilotColumn = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		_pilotColumn.AddThemeConstantOverride("separation", 8);
		margin.AddChild(_pilotColumn);
	}

	public void Sync(State run, StarSystemOrchestrator orchestrator)
	{
		_orchestrator = orchestrator;
		run.EnsurePlayerShipPortraits(run.PlayerParty.ShipIds);
		var runtime = orchestrator.RuntimeFor(State.PlayerFleetUnitId);
		var selectedShipId = PlayerFleetSelection.EnsureSelectedMember(
			orchestrator.Map,
			runtime,
			State.PlayerFleetUnitId,
			run.PlayerParty.ShipIds);

		foreach (var shipId in run.PlayerParty.ShipIds)
		{
			if (!run.ShipRegistry.TryGet(shipId, out var ship)
				|| !run.PlayerShipPortraitIds.TryGetValue(shipId, out var portraitId))
				continue;

			if (!_pilots.TryGetValue(shipId, out var pilot))
			{
				pilot = new ShipPilot();
				pilot.Selected += OnPilotSelected;
				_pilots[shipId] = pilot;
				_pilotColumn.AddChild(pilot);
			}

			pilot.SetState(
				shipId,
				portraitId,
				ship,
				string.Equals(shipId, selectedShipId, StringComparison.Ordinal));
		}

		foreach (var shipId in _pilots.Keys.Except(run.PlayerParty.ShipIds, StringComparer.Ordinal).ToArray())
		{
			var removed = _pilots[shipId];
			removed.Selected -= OnPilotSelected;
			removed.QueueFree();
			_pilots.Remove(shipId);
		}
	}

	private void OnPilotSelected(string shipId)
	{
		_orchestrator?.TryCommitPlayerInput(
			new SelectActiveFleetShipAction(State.PlayerFleetUnitId, shipId));
	}
}
