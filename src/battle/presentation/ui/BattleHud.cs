using Godot;
using GrimSpace.Application;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Objectives;
using GrimSpace.Components;
using GrimSpace.Presentation.Menu;
using GrimSpace.Math.Grid;

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
	public event Action<ESpatialOrientation?>? MountFaceHovered;

	public BattleCommandHud CommandHud { get; private set; } = null!;
	public TurnFlowBar TurnFlowBar { get; private set; } = null!;
	public BattleVictoryOverlay VictoryOverlay { get; private set; } = null!;
	public BattlePauseMenuOverlay PauseMenu { get; private set; } = null!;

	public bool IsPauseMenuOpen =>
		PauseMenu.Visible || PauseMenu.IsReportDialogOpen || PauseMenu.IsSettingsOpen;
	public bool IsMountSelectorOpen => _mountSelectorLayer.Visible;
	public Rect2? TurnFlowTransitionRect =>
		_turnFlowLayer.Layer > 90 && _turnFlowLayer.Visible ? TurnFlowBar.GetGlobalRect() : null;

	private bool _strategicBattle;

	private Control _turnBadge = null!;
	private Label _turnLabel = null!;
	private CanvasLayer _phaseLayer = null!;
	private ColorRect _planningVignette = null!;
	private Tween? _planningVignetteTween;
	private bool _planningVignetteTargetVisible;
	private CanvasLayer _topHud = null!;
	private CanvasLayer _turnFlowLayer = null!;
	private CanvasLayer _bottomHud = null!;
	private BattleControlHints _controlHints = null!;
	private bool _simulationVisualsEnabled;
	private bool _turnFlowTransitionActive;
	private CanvasLayer _actionLogLayer = null!;
	private MarginContainer _actionLogHost = null!;
	private ActionLogPanel _actionLogPanel = null!;
	private CanvasLayer _mountSelectorLayer = null!;
	private MountFaceSelector _mountFaceSelector = null!;

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

		var turnRow = new HBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
		};
		turnRow.AddThemeConstantOverride("separation", 8);
		topMargin.AddChild(turnRow);

		_turnBadge = new Control
		{
			CustomMinimumSize = new Vector2(157f, 46f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		var turnBadgeMaterial = new ShaderMaterial
		{
			Shader = GD.Load<Shader>("res://assets/ui/components/runtime/button_palette.gdshader")
				?? throw new InvalidOperationException("Could not load technical button palette shader."),
		};
		turnBadgeMaterial.SetShaderParameter("surface_color", HudPalettes.PurpleRed.DeepSurface);
		turnBadgeMaterial.SetShaderParameter("edge_color", HudPalettes.PurpleRed.Highlight);
		turnBadgeMaterial.SetShaderParameter("palette_mix", 1f);
		var turnBadgeBackground = new TextureRect
		{
			Texture = GD.Load<Texture2D>("res://assets/ui/components/runtime/button_technical_base.png")
				?? throw new InvalidOperationException("Could not load technical button background."),
			Material = turnBadgeMaterial,
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		_turnBadge.AddChild(turnBadgeBackground);
		_turnLabel = new Label
		{
			Text = BattleHudCopy.Turn(1),
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			OffsetLeft = 16f,
			OffsetRight = -16f,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			ThemeTypeVariation = "Turn",
			Modulate = HudPalettes.PurpleRed.Primary,
		};
		_turnBadge.AddChild(_turnLabel);
		turnRow.AddChild(_turnBadge);
		_controlHints = new BattleControlHints();
		turnRow.AddChild(_controlHints);

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
		AddChild(_topHud);
		_turnFlowLayer = new CanvasLayer { Layer = 10 };
		_turnFlowLayer.AddChild(flowCenter);
		AddChild(_turnFlowLayer);

		_bottomHud = new CanvasLayer { Layer = 10 };

		CommandHud = new BattleCommandHud
		{
			AnchorLeft = 0.5f,
			AnchorTop = 1f,
			AnchorRight = 0.5f,
			AnchorBottom = 1f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical = Control.GrowDirection.Begin,
		};
		_bottomHud.AddChild(CommandHud);

		AddChild(_bottomHud);
		BuildMountSelector();

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

		VictoryOverlay = new BattleVictoryOverlay();
		AddChild(VictoryOverlay);

		PauseMenu = new BattlePauseMenuOverlay();
		PauseMenu.VisibilityChanged += UpdateTurnFlowLayer;
		PauseMenu.ContinueRequested += ClosePauseMenu;
		PauseMenu.RetireRequested += () => RetireRequested?.Invoke();
		PauseMenu.RestartRequested += () => RestartRequested?.Invoke();
		PauseMenu.MainMenuRequested += () => MainMenuRequested?.Invoke();
		AddChild(PauseMenu);

		var battleTheme = HudThemes.Load(HudThemeFamily.Battle);
		topMargin.Theme = battleTheme;
		_actionLogHost.Theme = battleTheme;
		PauseMenu.ApplyTheme(MenuUi.Theme);
		CommandHud.Theme = battleTheme;
		_controlHints.Theme = battleTheme;
	}

	public void SetSimulationVisualsEnabled(bool enabled)
	{
		_simulationVisualsEnabled = enabled;
		_controlHints.Visible = enabled && _bottomHud.Visible;
	}

	public void SetTurnFlowTransitionActive(bool active)
	{
		_turnFlowTransitionActive = active;
		UpdateTurnFlowLayer();
	}

	private void UpdateTurnFlowLayer() =>
		_turnFlowLayer.Layer = _turnFlowTransitionActive && !PauseMenu.Visible ? 91 : 10;

	private void BuildMountSelector()
	{
		_mountSelectorLayer = new CanvasLayer
		{
			Layer = 20,
			Visible = false,
		};
		_mountFaceSelector = new MountFaceSelector
		{
			Name = "MountFaceSelector",
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical = Control.GrowDirection.Both,
		};
		_mountFaceSelector.FaceHovered += face => MountFaceHovered?.Invoke(face);
		_mountSelectorLayer.AddChild(_mountFaceSelector);
		AddChild(_mountSelectorLayer);
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
		AddChild(_phaseLayer);
	}

	public void TogglePauseMenu() => PauseMenu.Visible = !PauseMenu.Visible;

	public void ClosePauseMenu() => PauseMenu.Visible = false;

	public void ShowMountSelector(
		Vector2 center,
		IEnumerable<MountFaceSelectorOption> options)
	{
		_mountSelectorLayer.Visible = true;
		_mountFaceSelector.ShowProjected(center, options);
	}

	public void HideMountSelector()
	{
		_mountSelectorLayer.Visible = false;
		_mountFaceSelector.Dismiss();
	}

	public (ESpatialOrientation? Face, bool IsOutside) PickMountFace(Vector2 position) =>
		_mountFaceSelector.Pick(position);

	public void SetMountFaceHover(ESpatialOrientation? face) =>
		_mountFaceSelector.SetHoveredFace(face);

	public void NudgeMountSelector() => _mountFaceSelector.NudgeAttention();

	public void SetStrategicBattle(bool strategic)
	{
		_strategicBattle = strategic;
		PauseMenu.SetRestartEnabled(!strategic);
	}

	public void Apply(
		PresentationFrame frame,
		EBattlePhase phase,
		bool allowEndTurn,
		BattleVictorySummary? victorySummary = null,
		CommandHighlights highlights = default)
	{
		var hideHud = frame.ShowOutcomeOverlay;
		if (hideHud)
			ClosePauseMenu();

		FadePhaseVisual(
			_planningVignette,
			!hideHud && phase == EBattlePhase.PlayerTurn,
			ref _planningVignetteTargetVisible,
			ref _planningVignetteTween);
		_topHud.Visible = !hideHud;
		_turnFlowLayer.Visible = !hideHud;
		_turnLabel.Text = BattleHudCopy.Turn(frame.TurnNumber);

		var focusState = frame.FocusState;

		_actionLogPanel.SetEntries(frame.ActionLogEntries);
		_actionLogLayer.Visible = !hideHud && GameSettings.ShowActionLog;

		VictoryOverlay.Visible = frame.ShowOutcomeOverlay;
		if (frame.ShowOutcomeOverlay)
		{
			if (victorySummary is null || frame.Outcome is not (EBattleResult.Win or EBattleResult.Lose))
				throw new InvalidOperationException("A finished battle requires a win or defeat scorecard.");

			VictoryOverlay.Sync(victorySummary, _strategicBattle, frame.Outcome);
		}

		_bottomHud.Visible = !hideHud;
		_controlHints.Apply(frame);
		_controlHints.Visible = _simulationVisualsEnabled && !hideHud;
		if (hideHud)
			return;

		CommandHud.ApplyResources(
			focusState.CurrentActionPoints,
			focusState.BaseActionPoints,
			focusState.CurrentManeuverPoints,
			focusState.BaseManeuverPoints,
			focusState);
		CommandHud.ApplyDefenses(
			focusState.CurrentHullPoints,
			focusState.BaseHullPoints,
			focusState.CurrentShieldPoints,
			focusState.Loadout.BaseShieldPoints);
		CommandHud.ApplyPilotPortrait(focusState.PilotPortraitId);
		CommandHud.StatusBar.Apply(focusState);
		CommandHud.ApplyMainActions(
			frame.Mode,
			frame.CanAct,
			focusState,
			frame.Abilities,
			frame.TargetPick,
			highlights.Ability);
		CommandHud.ApplyCommands(
			frame.IsInspecting,
			frame.CanAct,
			frame.CanFocusCamera,
			frame.RangeEnemy is not null,
			frame.CanUndo,
			allowEndTurn,
			highlights.Undo,
			highlights.Execute);
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
