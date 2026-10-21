using Godot;
using GrimSpace.World.StarSystem.Presentation.Facilities;

namespace GrimSpace.Presentation.Dev.Hud;

public sealed partial class ShipRecruitmentHudPreview : Control
{
	public override void _Ready()
	{
		UiHudPreviewChrome.Attach(
			this,
			"Ship recruitment HUD preview",
			"Trade hub ship broker offers");

		var dev = UiHudDevContext.Create();
		var overlay = new ShipRecruitmentHudOverlay();
		UiHudPreviewChrome.MountOverlay(this, overlay);
		overlay.Open(dev.Run, "Preview ship broker");
	}
}
