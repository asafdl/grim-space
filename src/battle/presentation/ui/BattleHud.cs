using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

/// <summary>
/// Façade over combat HUD widgets driven by <see cref="PresentationFrame"/>.
/// </summary>
public partial class BattleHud : Node
{
	public ActionBar ActionBar { get; private set; } = null!;
	public ShipOrientationHud OrientationHud { get; private set; } = null!;
	public BattleOutcomeOverlay OutcomeOverlay { get; private set; } = null!;

	private Label _hintLabel = null!;
	private CanvasLayer _actionLogLayer = null!;
	private ActionLogPanel _actionLogPanel = null!;

	public void Build()
	{
		var canvas = new CanvasLayer();
		_hintLabel = new Label { Position = new Vector2(16, 16) };
		canvas.AddChild(_hintLabel);
		AddChild(canvas);

		ActionBar = new ActionBar();
		AddChild(ActionBar);

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

		OrientationHud = new ShipOrientationHud();
		AddChild(OrientationHud);
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

		ActionBar.Visible = !frame.ShowOutcomeOverlay;
		if (!frame.ShowOutcomeOverlay)
		{
			ActionBar.SetMode(frame.Mode);
			ActionBar.Configure(
				frame.FlakAvailable,
				frame.RailgunAvailable,
				frame.TorpedoAvailable,
				frame.CanAct);
		}

		if (frame.ShowOutcomeOverlay)
		{
			OrientationHud.Visible = false;
			return;
		}

		OrientationHud.Visible = frame.CanAct
			&& frame.Mode is not (EPlayerMode.Flak or EPlayerMode.Torpedo);
	}
}
