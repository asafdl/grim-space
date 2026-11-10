using Godot;
using GrimSpace.Application;
using GrimSpace.Components;
using GrimSpace.Run;
using GrimSpace.Ui.Components;
using GrimSpace.World.StarSystem;
using GrimSpace.Pilots.Barks;
using GrimSpace.World.StarSystem.Presentation.Scene;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public partial class PlayerFleetHud : MarginContainer
{
	private readonly Dictionary<string, ShipPilot> _pilots = new(StringComparer.Ordinal);
	private VBoxContainer _pilotColumn = null!;
	private PilotBarkDirector? _barks;
	private PilotSpeechHost? _speech;

	public void BindBarks(PilotBarkDirector director, PilotSpeechHost speech)
	{
		_barks = director;
		_speech = speech;
	}

	public override void _ExitTree()
	{
		foreach (var pilotId in _pilots.Keys)
			_barks?.UnregisterPilot(pilotId);
	}

	/// <summary>Open engagement deployment; roster portraits drag into it while it is visible.</summary>
	public DeploymentSlots? Deployment { get; set; }

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

		_pilotColumn = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		_pilotColumn.AddThemeConstantOverride("separation", 8);
		content.AddChild(_pilotColumn);
	}

	public void Sync(State run)
	{

		var rosterPilotIds = new HashSet<string>(StringComparer.Ordinal);
		var index = 0;
		foreach (var pilot in run.PilotRegistry.All)
		{
			var ship = pilot.AssignedShipId is { } shipId
				? run.ItemInventory.Get(shipId)
				: null;
			rosterPilotIds.Add(pilot.Id);
			if (!_pilots.TryGetValue(pilot.Id, out var card))
			{
				card = new ShipPilot();
				card.PilotSheetRequested += OnPilotSheetRequested;
				_pilots[pilot.Id] = card;
				_pilotColumn.AddChild(card);
				BindDeploymentDrag(card.Portrait, pilot.Id);
				if (_barks is not null && _speech is { } speech)
				{
					var pilotId = pilot.Id;
					_barks.RegisterPilot(pilotId, line => speech.Say(pilotId, line));
				}
			}

			_pilotColumn.MoveChild(card, index++);
			card.SetState(
				pilot.Id,
				pilot.Name,
				pilot.PortraitId,
				pilot.ExperiencePoints,
				pilot.ClaimedLevel,
				PilotSkillAvailability.AvailableToLearn(pilot).Count > 0,
				ship);
		}

		foreach (var pilotId in _pilots.Keys.Except(rosterPilotIds, StringComparer.Ordinal).ToArray())
		{
			var removed = _pilots[pilotId];
			removed.PilotSheetRequested -= OnPilotSheetRequested;
			removed.QueueFree();
			_barks?.UnregisterPilot(pilotId);
			_pilots.Remove(pilotId);
		}
	}

	public Control? PortraitAnchorFor(string pilotId) =>
		_pilots.TryGetValue(pilotId, out var card) ? card.Portrait : null;

	private void BindDeploymentDrag(Control portrait, string pilotId)
	{
		portrait.SetDragForwarding(
			Callable.From<Vector2, Variant>(position => OpenDeployment() is { } deployment
				? deployment.BeginDrag(portrait, pilotId, position, new Rect2(Vector2.Zero, portrait.Size))
				: default),
			Callable.From<Vector2, Variant, bool>((_, data) => OpenDeployment()?.IsPilot(data) == true),
			Callable.From<Vector2, Variant>((_, data) => OpenDeployment()?.Remove(data.AsString())));
		portrait.GuiInput += @event =>
		{
			if (@event is InputEventMouseButton { DoubleClick: true, ButtonIndex: MouseButton.Left })
				OpenDeployment()?.Deploy(pilotId);
		};
	}

	private DeploymentSlots? OpenDeployment() =>
		Deployment is { } deployment && deployment.IsVisibleInTree() ? deployment : null;

	private void OnPilotSheetRequested(string pilotId) =>
		PilotSheetRequested?.Invoke(pilotId);
}
