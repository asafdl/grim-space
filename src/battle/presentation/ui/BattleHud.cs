using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

/// <summary>
/// Façade over combat HUD widgets driven by <see cref="PresentationFrame"/>.
/// </summary>
public partial class BattleHud : Node
{
	public ActionBar ActionBar { get; private set; } = null!;
	public OrientationBar OrientationBar { get; private set; } = null!;
	public BattleOutcomeOverlay OutcomeOverlay { get; private set; } = null!;

	private Label _hintLabel = null!;
	private CanvasLayer _bottomHud = null!;
	private CanvasLayer _actionLogLayer = null!;
	private ActionLogPanel _actionLogPanel = null!;

	public void Build()
	{
		var canvas = new CanvasLayer();
		_hintLabel = new Label { Position = new Vector2(16, 16) };
		canvas.AddChild(_hintLabel);
		AddChild(canvas);

		_bottomHud = new CanvasLayer { Layer = 10 };
		var margin = new MarginContainer
		{
			AnchorsPreset = (int)Control.LayoutPreset.BottomWide,
			AnchorTop = 1f,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			OffsetTop = -160f,
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

		ActionBar = new ActionBar
		{
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		row.AddChild(ActionBar);

		AddChild(_bottomHud);

		_actionLogLayer = new CanvasLayer { Layer = 15 };
		var actionLogHost = new MarginContainer
		{
			AnchorsPreset = (int)Control.LayoutPreset.RightWide,
			AnchorLeft = 1f,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			OffsetLeft = -320f,
			OffsetTop = 56f,
			OffsetBottom = -88f,
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
		_hintLabel.Visible = !frame.ShowOutcomeOverlay;
		_hintLabel.Text = frame.HintText;

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
	}
}
