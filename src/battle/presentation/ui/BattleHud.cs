using Godot;
using GrimSpace.Battle.Weapons;

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

	public void Build()
	{
		var canvas = new CanvasLayer();
		_hintLabel = new Label { Position = new Vector2(16, 16) };
		canvas.AddChild(_hintLabel);
		AddChild(canvas);

		ActionBar = new ActionBar();
		AddChild(ActionBar);

		OutcomeOverlay = new BattleOutcomeOverlay();
		AddChild(OutcomeOverlay);

		OrientationHud = new ShipOrientationHud();
		AddChild(OrientationHud);
	}

	public void Apply(PresentationFrame frame)
	{
		_hintLabel.Visible = !frame.ShowOutcomeOverlay;
		_hintLabel.Text = frame.HintText;

		OutcomeOverlay.SetVisible(frame.ShowOutcomeOverlay);
		if (frame.ShowOutcomeOverlay)
			OutcomeOverlay.SetOutcome(frame.PlayerWon);

		ActionBar.Visible = !frame.ShowOutcomeOverlay;
		if (!frame.ShowOutcomeOverlay)
		{
			ActionBar.SetMode(frame.Mode, frame.MissileMount);
			ActionBar.Configure(
				frame.MissilesRemaining,
				CombatConfig.MissilesPerTurn,
				frame.FlakAvailable,
				frame.CanAct);
		}

		if (frame.ShowOutcomeOverlay)
		{
			OrientationHud.Show(false);
			return;
		}

		OrientationHud.Show(frame.CanAct && !frame.MissileAimActive && frame.Mode != EPlayerMode.Flak);
	}
}
