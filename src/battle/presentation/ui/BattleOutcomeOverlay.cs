using Godot;
using GrimSpace.Battle.Objectives;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class BattleOutcomeOverlay : CanvasLayer
{
	public event Action? ResetRequested;

	private Control _root = null!;
	private Label _title = null!;
	private PanelContainer _panel = null!;
	private ActionLogPanel _actionLog = null!;

	public BattleOutcomeOverlay()
	{
		Layer = 20;
		Build();
		Visible = false;
	}

	public void SetOutcome(EBattleResult result, IReadOnlyList<string> actionLogLines)
	{
		_title.Text = BattleHudCopy.OutcomeTitle(result);
		_actionLog.SetLines(actionLogLines);
	}

	private void Build()
	{
		_root = new Control
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical = Control.GrowDirection.Both,
			MouseFilter = Control.MouseFilterEnum.Stop,
		};
		AddChild(_root);

		var backdrop = new ColorRect
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical = Control.GrowDirection.Both,
			Color = new Color(0f, 0f, 0f, 0.55f),
		};
		_root.AddChild(backdrop);

		var center = new CenterContainer
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical = Control.GrowDirection.Both,
		};
		_root.AddChild(center);

		var panel = new PanelContainer();
		_panel = panel;
		center.AddChild(panel);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 28);
		margin.AddThemeConstantOverride("margin_right", 28);
		margin.AddThemeConstantOverride("margin_top", 24);
		margin.AddThemeConstantOverride("margin_bottom", 24);
		panel.AddChild(margin);

		var content = new VBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
		};
		content.AddThemeConstantOverride("separation", 16);
		margin.AddChild(content);

		_title = new Label
		{
			Text = BattleHudCopy.OutcomeWin,
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		_title.AddThemeFontSizeOverride("font_size", 36);
		content.AddChild(_title);

		_actionLog = new ActionLogPanel
		{
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
		};
		content.AddChild(_actionLog);

		var resetButton = new Button
		{
			Text = BattleHudCopy.Reset,
			CustomMinimumSize = new Vector2(140, 44),
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
		};
		resetButton.Pressed += () => ResetRequested?.Invoke();
		content.AddChild(resetButton);

		Callable.From(ConnectViewportLayout).CallDeferred();
	}

	private void ConnectViewportLayout()
	{
		GetViewport().SizeChanged += OnViewportSizeChanged;
		LayoutPanel();
	}

	private void OnViewportSizeChanged() => LayoutPanel();

	private void LayoutPanel()
	{
		var viewport = GetViewport();
		_panel.CustomMinimumSize = new Vector2(
			UiScale.Px(520f, viewport),
			UiScale.Px(460f, viewport));
		_actionLog.CustomMinimumSize = new Vector2(
			UiScale.Px(440f, viewport),
			UiScale.Px(280f, viewport));
		_title.AddThemeFontSizeOverride("font_size", UiScale.Font(36, viewport));
	}
}
