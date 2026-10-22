using Godot;
using GrimSpace.Application;
using GrimSpace.Components;
using GrimSpace.Run;
using GrimSpace.Ui.Components;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public partial class PlayerFleetHud : MarginContainer
{
	private readonly Dictionary<string, ShipPilot> _pilots = new(StringComparer.Ordinal);
	private UiButton _fleetButton = null!;
	private VBoxContainer _pilotColumn = null!;
	private StarSystemOrchestrator? _orchestrator;

	public event Action? FleetManagementRequested;
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

		_fleetButton = new UiButton("", HudActionKind.Secondary)
		{
			TooltipText = "Manage owned ships and pilot assignments",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_fleetButton.SetVisual(UiButtonVisual.Technical);
		_fleetButton.CustomMinimumSize = new Vector2(200f, 46f);
		_fleetButton.Pressed += () => FleetManagementRequested?.Invoke();
		content.AddChild(_fleetButton);

		_pilotColumn = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		_pilotColumn.AddThemeConstantOverride("separation", 8);
		content.AddChild(_pilotColumn);
	}

	public void Sync(State run, StarSystemOrchestrator orchestrator)
	{
		_orchestrator = orchestrator;
		_fleetButton.Text =
			$"FLEET · {run.GetFieldedShipIds().Count}/{RunPilotRegistry.MaxActivePilots}  "
			+ $"[{GameInputBindings.Label("map_fleet_management")}]";
		var runtime = orchestrator.RuntimeFor(State.PlayerFleetUnitId);
		var selectedShipId = PlayerFleetSelection.EnsureSelectedMember(
			orchestrator.Map,
			runtime,
			State.PlayerFleetUnitId,
			run.GetFieldedShipIds());

		var activePilotIds = new HashSet<string>(StringComparer.Ordinal);
		foreach (var pilot in run.PilotRegistry.Active)
		{
			if (pilot.AssignedShipId is null
				|| !run.ItemInventory.TryGet(pilot.AssignedShipId, out var ship))
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
				string.Equals(pilot.AssignedShipId, selectedShipId, StringComparison.Ordinal));
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
