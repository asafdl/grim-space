using Godot;
using GrimSpace.Battle.Presentation.Ui;

namespace GrimSpace.Presentation.Dev.Hud;

public sealed partial class BattleVictoryHudPreview : Control
{
	[Export]
	public bool StrategicBattle { get; set; }

	private BattleVictoryOverlay _overlay = null!;

	public override void _Ready()
	{
		UiHudPreviewChrome.Attach(
			this,
			"Battle victory scorecard preview",
			"F6 to run · R replay · staggered row reveal + door SFX + XP bar fill");

		_overlay = new BattleVictoryOverlay();
		AddChild(_overlay);
		ShowOverlay();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.R })
			return;

		ShowOverlay();
		GetViewport().SetInputAsHandled();
	}

	private void ShowOverlay()
	{
		_overlay.HideOverlay();
		_overlay.Sync(SampleSummary(), StrategicBattle);
	}

	private static BattleVictorySummary SampleSummary() =>
		new([
			new BattleVictoryPilotRow(
				"Syndi Krauss",
				"robot_v1:1",
				"Fighter · T1",
				142,
				18,
				0,
				85,
				15,
				100,
				'A'),
			new BattleVictoryPilotRow(
				"Rust Bucket",
				"robot_v1:5",
				"Repurposed Miner · T0",
				67,
				44,
				12,
				42,
				40,
				82,
				'B'),
			new BattleVictoryPilotRow(
				"Vacuum Kid",
				"robot_v1:9",
				"Interceptor · T2",
				23,
				91,
				0,
				0,
				210,
				210,
				'D',
				IsScrapped: true),
		]);
}
