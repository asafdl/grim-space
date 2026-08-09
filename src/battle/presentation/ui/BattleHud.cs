using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

/// <summary>
/// Façade over combat HUD widgets driven by <see cref="PresentationFrame"/>.
/// </summary>
public partial class BattleHud : Node
{
	public ActionBar ActionBar { get; private set; } = null!;
	public ApBar ApBar { get; private set; } = null!;
	public HealthBar HealthBar { get; private set; } = null!;
	public OrientationBar OrientationBar { get; private set; } = null!;
	public UtilityBar UtilityBar { get; private set; } = null!;
	public BattleOutcomeOverlay OutcomeOverlay { get; private set; } = null!;

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

		_turnBadge = new PanelContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
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
			Text = "Turn 1",
			MouseFilter = Control.MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
		};
		_turnLabel.AddThemeFontSizeOverride("font_size", 16);
		_turnLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.88f));
		_turnBadge.AddChild(_turnLabel);
		topColumn.AddChild(_turnBadge);

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
			OffsetTop = -180f,
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

		OrientationBar = new OrientationBar
		{
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		row.AddChild(OrientationBar);

		var actionColumn = new VBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.End,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		actionColumn.AddThemeConstantOverride("separation", 6);
		row.AddChild(actionColumn);

		ApBar = new ApBar();
		actionColumn.AddChild(ApBar);

		ActionBar = new ActionBar
		{
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		actionColumn.AddChild(ActionBar);

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
			OffsetBottom = -180f,
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
	}

	public void Apply(PresentationFrame frame)
	{
		_topHud.Visible = !frame.ShowOutcomeOverlay;
		_turnLabel.Text = $"Turn {frame.TurnNumber}";
		HealthBar.Set(frame.ActorState);

		_actionLogPanel.SetLines(frame.ActionLogLines);
		_actionLogLayer.Visible = !frame.ShowOutcomeOverlay;

		OutcomeOverlay.Visible = frame.ShowOutcomeOverlay;
		if (frame.ShowOutcomeOverlay)
			OutcomeOverlay.SetOutcome(frame.Outcome, frame.ActionLogLines);

		_bottomHud.Visible = !frame.ShowOutcomeOverlay;
		if (frame.ShowOutcomeOverlay)
			return;

		var orientationAvailable = frame.CanAct
			&& frame.Mode is not (EPlayerMode.Flak or EPlayerMode.Torpedo);
		ActionBar.SetMode(frame.Mode);
		ActionBar.Configure(
			frame.FlakAvailable,
			frame.RailgunAvailable,
			frame.TorpedoAvailable,
			frame.CanAct);
		OrientationBar.Configure(orientationAvailable);
		UtilityBar.Configure(frame.CanFocusCamera, frame.CanUndo);
		ApBar.Set(frame.ActorState.ActionPoints, frame.ActorState.Stats.MaxAp);
	}
}
