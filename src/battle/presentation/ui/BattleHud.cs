using Godot;
using GrimSpace.Battle.Movement;

namespace GrimSpace.Battle.Presentation.Ui;

/// <summary>
/// Façade over combat HUD widgets driven by <see cref="PresentationFrame"/>.
/// </summary>
public partial class BattleHud : Node
{
	public event Action? RetireRequested;
	public event Action? RestartRequested;
	public event Action? MainMenuRequested;

	public ActionBar ActionBar { get; private set; } = null!;
	public HealthBar HealthBar { get; private set; } = null!;
	public ManeuverBar ManeuverBar { get; private set; } = null!;
	public UtilityBar UtilityBar { get; private set; } = null!;
	public BattleOutcomeOverlay OutcomeOverlay { get; private set; } = null!;
	public BattleIntroOverlay IntroOverlay { get; private set; } = null!;
	public BattlePauseMenuOverlay PauseMenu { get; private set; } = null!;

	public bool IsPauseMenuOpen => PauseMenu.Visible;

	private PanelContainer _turnBadge = null!;
	private Label _turnLabel = null!;
	private CanvasLayer _topHud = null!;
	private CanvasLayer _bottomHud = null!;
	private CanvasLayer _actionLogLayer = null!;
	private ActionLogPanel _actionLogPanel = null!;

	public void Build()
	{
		_topHud = new CanvasLayer { Layer = 10 };
		var topMargin = new MarginContainer
		{
			AnchorsPreset = (int)Control.LayoutPreset.TopLeft,
			GrowHorizontal = Control.GrowDirection.End,
			GrowVertical = Control.GrowDirection.End,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		topMargin.AddThemeConstantOverride("margin_left", 16);
		topMargin.AddThemeConstantOverride("margin_top", 16);
		_topHud.AddChild(topMargin);

		var topColumn = new VBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		topColumn.AddThemeConstantOverride("separation", 28);
		topMargin.AddChild(topColumn);

		var turnRow = new HBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
		};
		turnRow.AddThemeConstantOverride("separation", 8);
		topColumn.AddChild(turnRow);

		_turnBadge = new PanelContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		_turnBadge.AddThemeStyleboxOverride("panel", new StyleBoxFlat
		{
			BgColor = new Color(0.55f, 0.08f, 0.14f, 0.92f),
			BorderColor = new Color(0.9f, 0.25f, 0.35f, 0.95f),
			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,
			CornerRadiusTopLeft = 4,
			CornerRadiusTopRight = 4,
			CornerRadiusBottomRight = 4,
			CornerRadiusBottomLeft = 4,
			ContentMarginLeft = 12,
			ContentMarginRight = 12,
			ContentMarginTop = 6,
			ContentMarginBottom = 6,
		});
		_turnLabel = new Label
		{
			Text = BattleHudCopy.Turn(1),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
		};
		_turnLabel.AddThemeFontSizeOverride("font_size", 16);
		_turnLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.88f));
		_turnBadge.AddChild(_turnLabel);
		turnRow.AddChild(_turnBadge);

		HealthBar = new HealthBar
		{
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
		};
		topColumn.AddChild(HealthBar);
		AddChild(_topHud);

		_bottomHud = new CanvasLayer { Layer = 10 };
		var margin = new MarginContainer
		{
			AnchorsPreset = (int)Control.LayoutPreset.BottomWide,
			AnchorTop = 1f,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			OffsetTop = -220f,
			GrowHorizontal = Control.GrowDirection.Both,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		margin.AddThemeConstantOverride("margin_left", 16);
		margin.AddThemeConstantOverride("margin_right", 16);
		margin.AddThemeConstantOverride("margin_bottom", 14);
		_bottomHud.AddChild(margin);

		var center = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		margin.AddChild(center);

		var row = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		row.AddThemeConstantOverride("separation", 14);
		center.AddChild(row);

		var modeGroup = new ButtonGroup();

		ManeuverBar = new ManeuverBar(modeGroup)
		{
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		row.AddChild(ManeuverBar);

		ActionBar = new ActionBar(modeGroup)
		{
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		row.AddChild(ActionBar);

		UtilityBar = new UtilityBar
		{
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		row.AddChild(UtilityBar);

		AddChild(_bottomHud);

		_actionLogLayer = new CanvasLayer { Layer = 5 };
		var actionLogHost = new MarginContainer
		{
			AnchorsPreset = (int)Control.LayoutPreset.RightWide,
			AnchorLeft = 1f,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			OffsetLeft = -220f,
			OffsetTop = 56f,
			OffsetBottom = -220f,
			GrowHorizontal = Control.GrowDirection.Begin,
			GrowVertical = Control.GrowDirection.Both,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		actionLogHost.AddThemeConstantOverride("margin_right", 12);
		actionLogHost.AddThemeConstantOverride("margin_top", 0);
		actionLogHost.AddThemeConstantOverride("margin_bottom", 0);
		_actionLogPanel = new ActionLogPanel
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
		};
		actionLogHost.AddChild(_actionLogPanel);
		_actionLogLayer.AddChild(actionLogHost);
		AddChild(_actionLogLayer);

		OutcomeOverlay = new BattleOutcomeOverlay();
		AddChild(OutcomeOverlay);

		IntroOverlay = new BattleIntroOverlay();
		AddChild(IntroOverlay);

		PauseMenu = new BattlePauseMenuOverlay();
		PauseMenu.ContinueRequested += ClosePauseMenu;
		PauseMenu.RetireRequested += () => RetireRequested?.Invoke();
		PauseMenu.RestartRequested += () => RestartRequested?.Invoke();
		PauseMenu.MainMenuRequested += () => MainMenuRequested?.Invoke();
		AddChild(PauseMenu);
	}

	public void TogglePauseMenu() => PauseMenu.Visible = !PauseMenu.Visible;

	public void ClosePauseMenu() => PauseMenu.Visible = false;

	public void Apply(PresentationFrame frame)
	{
		var hideHud = frame.ShowOutcomeOverlay || frame.ShowIntroOverlay;
		if (hideHud)
			ClosePauseMenu();

		_topHud.Visible = !hideHud;
		_turnLabel.Text = BattleHudCopy.Turn(frame.TurnNumber);

		var focusState = frame.FocusState;
		HealthBar.Set(focusState);

		_actionLogPanel.SetLines(frame.ActionLogLines);
		_actionLogLayer.Visible = !hideHud;

		OutcomeOverlay.Visible = frame.ShowOutcomeOverlay;
		if (frame.ShowOutcomeOverlay)
			OutcomeOverlay.SetOutcome(frame.Outcome, frame.ActionLogLines);

		IntroOverlay.Visible = frame.ShowIntroOverlay;

		_bottomHud.Visible = !hideHud;
		if (hideHud)
			return;

		ManeuverBar.SetMode(frame.Mode);
		ManeuverBar.Configure(
			frame.CanAct,
			focusState.ActionPoints,
			focusState.MaxActionPoints,
			focusState.MomentumLevel,
			MomentumConfig.MaxLevel);

		var abilitySpecs = AbilityHudCatalog.ForUnit(focusState.Type);
		var abilitySlots = abilitySpecs
			.Select(spec => AbilityHudCatalog.BuildState(spec, focusState, frame.Abilities))
			.ToList();
		ActionBar.ApplyLayout(focusState.Type, abilitySpecs);
		ActionBar.SetMode(frame.Mode);
		ActionBar.Configure(frame.CanAct, frame.IsInspecting, abilitySlots);
		UtilityBar.Configure(frame.IsInspecting, frame.CanFocusCamera, frame.CanUndo);
	}

}
