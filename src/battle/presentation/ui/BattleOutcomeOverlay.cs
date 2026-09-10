using Godot;
using GrimSpace.Battle.Objectives;
using GrimSpace.Components;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class BattleOutcomeOverlay : CanvasLayer
{
	public event Action? ResetRequested;

	private Control _root = null!;
	private Label _title = null!;
	private ActionLogPanel _actionLog = null!;
	private Button _actionButton = null!;

	public BattleOutcomeOverlay()
	{
		Layer = 20;
		Build();
		Visible = false;
	}

	public void ApplyTheme(Theme theme) => _root.Theme = theme;

	public void SetOutcome(EBattleResult result, IReadOnlyList<string> actionLogLines, bool strategicBattle)
	{
		_title.Text = BattleHudCopy.OutcomeTitle(result);
		_actionLog.SetLines(actionLogLines);
		_actionButton.Text = strategicBattle
			? BattleHudCopy.ReturnToStarMap
			: BattleHudCopy.Reset;
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
			Color = HudStyles.ModalBackdrop,
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

		var panel = new PanelContainer
		{
			CustomMinimumSize = new Vector2(520, 460),
		};
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
			ThemeTypeVariation = "OutcomeTitle",
		};
		content.AddChild(_title);

		_actionLog = new ActionLogPanel
		{
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(440, 280),
		};
		content.AddChild(_actionLog);

		var resetButton = new Button
		{
			Text = BattleHudCopy.Reset,
			CustomMinimumSize = new Vector2(140, 44),
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
		};
		HudStyles.StyleButton(resetButton, HudActionKind.Primary);
		resetButton.Pressed += () => ResetRequested?.Invoke();
		_actionButton = resetButton;
		content.AddChild(resetButton);
	}
}
