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
	private const int ScrollStep = 96;

	private readonly Dictionary<string, ShipPilot> _pilots = new(StringComparer.Ordinal);
	private VBoxContainer _pilotColumn = null!;
	private ScrollContainer _scroll = null!;
	private Control _scrollUp = null!;
	private Control _scrollDown = null!;
	private PilotBarkDirector? _barks;
	private PilotSpeechHost? _speech;

	public void BindBarks(PilotBarkDirector director, PilotSpeechHost speech)
	{
		_barks = director;
		_speech = speech;
	}

	public override void _ExitTree()
	{
		GetViewport().SizeChanged -= FitHeight;
		foreach (var pilotId in _pilots.Keys)
			_barks?.UnregisterPilot(pilotId);
	}

	/// <summary>Open engagement deployment; roster portraits drag into it while it is visible.</summary>
	public DeploymentSlots? Deployment { get; set; }

	public event Action<string>? PilotSheetRequested;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;

		var margin = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
		foreach (var marginName in new[] { "margin_left", "margin_top", "margin_right", "margin_bottom" })
			margin.AddThemeConstantOverride(marginName, 8);
		AddChild(margin);

		var content = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		content.AddThemeConstantOverride("separation", 6);
		margin.AddChild(content);

		_scrollUp = HudCycleArrow.Create(
			-1, 76, "Scroll pilots up", () => _scroll.ScrollVertical -= ScrollStep, vertical: true);
		_scrollUp.Name = "ScrollUp";
		content.AddChild(_scrollUp);
		_scroll = new ScrollContainer
		{
			Name = "PilotScroll",
			CustomMinimumSize = new Vector2(76, 0),
			SizeFlagsVertical = SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever,
			MouseFilter = MouseFilterEnum.Stop,
		};
		content.AddChild(_scroll);
		_pilotColumn = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		_pilotColumn.AddThemeConstantOverride("separation", 8);
		_scroll.AddChild(_pilotColumn);
		_scrollDown = HudCycleArrow.Create(
			1, 76, "Scroll pilots down", () => _scroll.ScrollVertical += ScrollStep, vertical: true);
		_scrollDown.Name = "ScrollDown";
		content.AddChild(_scrollDown);
		var bar = _scroll.GetVScrollBar();
		bar.ValueChanged += _ => RefreshScrollButtons();
		bar.Changed += RefreshScrollButtons;
		GetViewport().SizeChanged += FitHeight;
		FitHeight();
		RefreshScrollButtons();
	}

	private void FitHeight() =>
		Size = new Vector2(Size.X, Mathf.Max(0, GetViewportRect().Size.Y - GlobalPosition.Y - HudStyles.Margin));

	private void RefreshScrollButtons()
	{
		var bar = _scroll.GetVScrollBar();
		_scrollUp.Modulate = bar.Value <= bar.MinValue ? new Color(1f, 1f, 1f, 0.25f) : Colors.White;
		_scrollDown.Modulate = bar.Value >= bar.MaxValue - bar.Page ? new Color(1f, 1f, 1f, 0.25f) : Colors.White;
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
					_barks.RegisterPilot(
						pilotId,
						line => speech.Say(pilotId, line),
						isVisible: () => PortraitAnchorFor(pilotId) is not null);
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

	public Control? PortraitAnchorFor(string pilotId)
	{
		if (!_pilots.TryGetValue(pilotId, out var card) || !card.IsVisibleInTree())
			return null;

		var portrait = card.Portrait;
		var rect = portrait.GetGlobalRect();
		return rect.HasArea()
			&& _scroll.GetGlobalRect().Encloses(rect)
			&& GetViewportRect().Encloses(rect)
				? portrait
				: null;
	}

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
