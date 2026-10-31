using Godot;
using GrimSpace.Presentation.Dev;

namespace GrimSpace.Presentation.Dev.Hud;

public sealed partial class DevMenuHudPreview : Control
{
	public override void _Ready()
	{
		UiHudPreviewChrome.Attach(
			this,
			"Dev menu HUD preview",
			"Layout only · actions are inert");

		var overlay = new DevMenuOverlay();
		UiHudPreviewChrome.MountOverlay(this, overlay);
		overlay.SetBattleActions(() => true, () => { }, () => { });
		overlay.SetResourceActions(() => true, () => { });
		overlay.SetPilotExperienceActions(() => true, () => { });
		overlay.SetDangerActions(() => true, () => { });
		overlay.SetMapActions(() => true, () => { }, () => true, () => { });
		overlay.Open();
	}
}
