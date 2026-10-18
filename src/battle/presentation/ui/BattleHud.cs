using Godot;
using GrimSpace.Application;
using GrimSpace.Battle.Movement;
using GrimSpace.Components;

namespace GrimSpace.Battle.Presentation.Ui;

/// <summary>
/// Façade over combat HUD widgets driven by <see cref="PresentationFrame"/>.
/// </summary>
public partial class BattleHud : Node
{
	private const double PhaseFadeInSeconds = 0.8;
	private const double PhaseFadeOutSeconds = 0.3;

	public event Action? RetireRequested;
	public event Action? RestartRequested;
	public event Action? MainMenuRequested;

	public ActionBar ActionBar { get; private set; } = null!;
	public HealthBar HealthBar { get; private set; } = null!;
	public ManeuverBar ManeuverBar { get; private set; } = null!;
	public UtilityBar UtilityBar { get; private set; } = null!;
	public TurnFlowBar TurnFlowBar { get; private set; } = null!;
	public BattleOutcomeOverlay OutcomeOverlay { get; private set; } = null!;
	public BattlePauseMenuOverlay PauseMenu { get; private set; } = null!;

	public bool IsPauseMenuOpen => PauseMenu.Visible || PauseMenu.IsReportDialogOpen;

	private bool _strategicBattle;

	private PanelContainer _turnBadge = null!;
	private Label _turnLabel = null!;
	private CanvasLayer _phaseLayer = null!;
	private ColorRect _planningVignette = null!;
	private CenterContainer _simulationPhaseHost = null!;
	private Label _simulationPhaseBanner = null!;
	private Tween? _planningVignetteTween;
	private Tween? _simulationPhaseBannerTween;
	private bool _planningVignetteTargetVisible;
	private bool _simulationPhaseBannerTargetVisible;
	private CanvasLayer _topHud = null!;
	private CanvasLayer _bottomHud = null!;
	private CanvasLayer _actionLogLayer = null!;
	private MarginContainer _actionLogHost = null!;
	private ActionLogPanel _actionLogPanel = null!;

	public void Build()
	{
		BuildPhaseOverlay();

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
		topColumn.AddThemeConstantOverride("separation", 16);
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
			ThemeTypeVariation = "Turn",
		};
		_turnBadge.AddChild(_turnLabel);
		turnRow.AddChild(_turnBadge);

		HealthBar = new HealthBar
		{
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
		};
		topColumn.AddChild(HealthBar);

		var flowCenter = new CenterContainer
		{
			AnchorsPreset = (int)Control.LayoutPreset.TopWide,
			AnchorRight = 1f,
			OffsetTop = 12f,
			OffsetBottom = 100f,
			GrowHorizontal = Control.GrowDirection.Both,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		TurnFlowBar = new TurnFlowBar();
		flowCenter.AddChild(TurnFlowBar);
		_topHud.AddChild(flowCenter);
		_topHud.AddChild(_simulationPhaseHost);
		AddChild(_topHud);

		_bottomHud = new CanvasLayer { Layer = 10 };

		var margin = new MarginContainer
		{
			AnchorsPreset = (int)Control.LayoutPreset.BottomWide,
			AnchorTop = 1f,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			OffsetTop = -250f,
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
			Alignment = BoxContainer.AlignmentMode.Begin,
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
		_actionLogHost = new MarginContainer
		{
			AnchorsPreset = (int)Control.LayoutPreset.RightWide,
			AnchorLeft = 1f,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			OffsetLeft = -300f,
			OffsetTop = 56f,
			OffsetBottom = -250f,
			GrowHorizontal = Control.GrowDirection.Begin,
			GrowVertical = Control.GrowDirection.Both,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		_actionLogHost.AddThemeConstantOverride("margin_right", 12);
		_actionLogPanel = new ActionLogPanel
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
		};
		_actionLogHost.AddChild(_actionLogPanel);
		_actionLogLayer.AddChild(_actionLogHost);
		AddChild(_actionLogLayer);

		OutcomeOverlay = new BattleOutcomeOverlay();
		AddChild(OutcomeOverlay);

		PauseMenu = new BattlePauseMenuOverlay();
		PauseMenu.ContinueRequested += ClosePauseMenu;
		PauseMenu.RetireRequested += () => RetireRequested?.Invoke();
		PauseMenu.RestartRequested += () => RestartRequested?.Invoke();
		PauseMenu.MainMenuRequested += () => MainMenuRequested?.Invoke();
		AddChild(PauseMenu);

		var battleTheme = HudThemes.Load(HudThemeFamily.Battle);
		topMargin.Theme = battleTheme;
		margin.Theme = battleTheme;
		_actionLogHost.Theme = battleTheme;
		OutcomeOverlay.ApplyTheme(battleTheme);
		PauseMenu.ApplyTheme(battleTheme);
		_simulationPhaseBanner.Theme = battleTheme;
	}

	private void BuildPhaseOverlay()
	{
		_phaseLayer = new CanvasLayer { Layer = 9 };
		_planningVignette = new ColorRect
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical = Control.GrowDirection.Both,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Material = new ShaderMaterial
			{
				Shader = new Shader
				{
					Code =
						"""
						shader_type canvas_item;

						void fragment() {
							vec2 edge_distance = min(UV, vec2(1.0) - UV);
							float nearest_edge = min(edge_distance.x, edge_distance.y);
							float strength = 1.0 - smoothstep(0.0, 0.14, nearest_edge);
							COLOR *= vec4(0.08, 0.42, 0.9, 0.24 * strength);
						}
						""",
				},
			},
			Modulate = new Color(1f, 1f, 1f, 0f),
			Visible = false,
		};
		_phaseLayer.AddChild(_planningVignette);

		_simulationPhaseHost = new CenterContainer
		{
			AnchorsPreset = (int)Control.LayoutPreset.TopWide,
			AnchorRight = 1f,
			OffsetTop = 104f,
			OffsetBottom = 146f,
			GrowHorizontal = Control.GrowDirection.Both,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		_simulationPhaseBanner = new Label
		{
			Text = BattleHudCopy.SimulationPhase,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
			Modulate = new Color(1f, 1f, 1f, 0f),
			Visible = false,
		};
		_simulationPhaseBanner.AddThemeFontSizeOverride("font_size", 18);
		_simulationPhaseBanner.AddThemeConstantOverride("outline_size", 4);
		_simulationPhaseBanner.AddThemeColorOverride(
			"font_color",
			new Color(0.48f, 0.82f, 0.82f, 0.86f));
		_simulationPhaseBanner.AddThemeColorOverride(
			"font_outline_color",
			new Color(0.02f, 0.05f, 0.07f, 0.9f));
		_simulationPhaseHost.AddChild(_simulationPhaseBanner);
		AddChild(_phaseLayer);
	}

	public void TogglePauseMenu() => PauseMenu.Visible = !PauseMenu.Visible;

	public void ClosePauseMenu() => PauseMenu.Visible = false;

	public void SetStrategicBattle(bool strategic)
	{
		_strategicBattle = strategic;
		PauseMenu.SetRestartEnabled(!strategic);
	}

	public void Apply(PresentationFrame frame, EBattlePhase phase, bool allowEndTurn)
	{
		var hideHud = frame.ShowOutcomeOverlay;
		if (hideHud)
			ClosePauseMenu();

		FadePhaseVisual(
			_planningVignette,
			!hideHud && phase == EBattlePhase.PlayerTurn,
			ref _planningVignetteTargetVisible,
			ref _planningVignetteTween);
		FadePhaseVisual(
			_simulationPhaseBanner,
			!hideHud && phase == EBattlePhase.PlayerTurn,
			ref _simulationPhaseBannerTargetVisible,
			ref _simulationPhaseBannerTween);
		_topHud.Visible = !hideHud;
		_turnLabel.Text = BattleHudCopy.Turn(frame.TurnNumber);

		var focusState = frame.FocusState;
		HealthBar.Set(focusState);

		_actionLogPanel.SetEntries(frame.ActionLogEntries);
		_actionLogLayer.Visible = !hideHud && GameSettings.ShowActionLog;

		OutcomeOverlay.Visible = frame.ShowOutcomeOverlay;
		if (frame.ShowOutcomeOverlay)
			OutcomeOverlay.SetOutcome(frame.Outcome, frame.ActionLogEntries, _strategicBattle);

		_bottomHud.Visible = !hideHud;
		if (hideHud)
			return;

		ManeuverBar.SetMode(frame.Mode);
		ManeuverBar.Configure(
			frame.CanAct,
			focusState.ActionPoints,
			focusState.MaxActionPoints,
			focusState.ManeuverPoints,
			focusState.MaxManeuverPoints);

		var abilitySpecs = AbilityHudCatalog.ForDisplayState(focusState);
		var abilitySlots = abilitySpecs
			.Select(spec => AbilityHudCatalog.BuildState(spec, focusState, frame.Abilities))
			.ToList();
		ActionBar.ApplyLayout(abilitySpecs);
		ActionBar.SetMode(frame.Mode);
		ActionBar.Configure(frame.CanAct, frame.IsInspecting, abilitySlots, allowEndTurn);
		ManeuverBar.RefreshBindingLabels();
		ActionBar.RefreshBindingLabels();
		UtilityBar.RefreshBindingLabels();
		ActionBar.InstructionBar.Apply(frame.Instruction);
		UtilityBar.Configure(frame.IsInspecting, frame.CanFocusCamera, frame.CanUndo);
	}

	private Tween? FadePhaseVisual(
		Control visual,
		bool visible,
		ref bool targetVisible,
		ref Tween? tween)
	{
		if (targetVisible == visible)
			return tween;

		targetVisible = visible;
		tween?.Kill();
		visual.Visible = true;
		tween = CreateTween();
		tween.SetEase(Tween.EaseType.InOut);
		tween.SetTrans(Tween.TransitionType.Sine);
		tween.TweenProperty(
			visual,
			"modulate:a",
			visible ? 1f : 0f,
			visible ? PhaseFadeInSeconds : PhaseFadeOutSeconds);
		if (!visible)
			tween.TweenCallback(Callable.From(() => visual.Visible = false));
		return tween;
	}

	public void SetTurnFlowTimeline(IReadOnlyList<TurnFlowEntry> timeline) =>
		TurnFlowBar.SetTimeline(timeline);

	public void SetTurnFlowIndex(int index) => TurnFlowBar.SetIndex(index);
}
