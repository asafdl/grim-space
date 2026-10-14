using Godot;
using GrimSpace.Components;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public partial class PlayerFleetHud : MarginContainer
{
	private readonly Dictionary<string, ShipPilot> _pilots = new(StringComparer.Ordinal);
	private VBoxContainer _pilotColumn = null!;
	private StarSystemOrchestrator? _orchestrator;

	public event Action<string>? PilotSheetRequested;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;

		var margin = new MarginContainer();
		foreach (var marginName in new[] { "margin_left", "margin_top", "margin_right", "margin_bottom" })
			margin.AddThemeConstantOverride(marginName, 8);
		AddChild(margin);

		var content = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		content.AddThemeConstantOverride("separation", 6);
		margin.AddChild(content);

		var title = new Label
		{
			Text = "CREW",
			HorizontalAlignment = HorizontalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		HudStyles.ApplyTextRole(title, HudTextRole.Metadata);
		content.AddChild(title);

		_pilotColumn = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		_pilotColumn.AddThemeConstantOverride("separation", 8);
		content.AddChild(_pilotColumn);
	}

	public void Sync(State run, StarSystemOrchestrator orchestrator)
	{
		_orchestrator = orchestrator;
		var runtime = orchestrator.RuntimeFor(State.PlayerFleetUnitId);
		var selectedShipId = PlayerFleetSelection.EnsureSelectedMember(
			orchestrator.Map,
			runtime,
			State.PlayerFleetUnitId,
			run.PlayerParty.ShipIds);

		var activePilotIds = new HashSet<string>(StringComparer.Ordinal);
		foreach (var shipId in run.PlayerParty.ShipIds)
		{
			if (!run.ShipRegistry.TryGet(shipId, out var ship)
				|| !run.PilotRegistry.TryGetForShip(shipId, out var pilot))
				continue;

			activePilotIds.Add(pilot.Id);
			if (!_pilots.TryGetValue(pilot.Id, out var card))
			{
				card = new ShipPilot();
				card.Selected += OnPilotSelected;
				card.PilotSheetRequested += OnPilotSheetRequested;
				_pilots[pilot.Id] = card;
				_pilotColumn.AddChild(card);
			}

			card.SetState(
				pilot.Id,
				pilot.Name,
				pilot.PortraitId,
				pilot.ExperiencePoints,
				pilot.ClaimedLevel,
				PilotSkillAvailability.AvailableToLearn(pilot).Count > 0,
				ship,
				string.Equals(shipId, selectedShipId, StringComparison.Ordinal));
		}

		foreach (var pilotId in _pilots.Keys.Except(activePilotIds, StringComparer.Ordinal).ToArray())
		{
			var removed = _pilots[pilotId];
			removed.Selected -= OnPilotSelected;
			removed.PilotSheetRequested -= OnPilotSheetRequested;
			removed.QueueFree();
			_pilots.Remove(pilotId);
		}
	}

	private void OnPilotSelected(string shipId)
	{
		_orchestrator?.TryCommitPlayerInput(
			new SelectActiveFleetShipAction(State.PlayerFleetUnitId, shipId));
	}

	private void OnPilotSheetRequested(string pilotId) =>
		PilotSheetRequested?.Invoke(pilotId);
}
