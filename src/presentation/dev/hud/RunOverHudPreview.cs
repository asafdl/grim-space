using Godot;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.Presentation.Ui;

namespace GrimSpace.Presentation.Dev.Hud;

public sealed partial class RunOverHudPreview : Control
{
	private RunOverHudOverlay _overlay = null!;

	public override void _Ready()
	{
		UiHudPreviewChrome.Attach(
			this,
			"Run Over HUD preview",
			"F6 to run · R reopen · Main Menu dismisses then reopens");

		_overlay = new RunOverHudOverlay();
		_overlay.MainMenuRequested += ShowOverlay;
		UiHudPreviewChrome.MountOverlay(this, _overlay);
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
		_overlay.Close();
		_overlay.Open(new RunStatisticsSummary(
			ContractsCompleted: 7,
			BattlesWon: 4,
			BattlesLost: 2,
			CreditsEarned: 1850,
			CreditsRemaining: 240));
	}
}
