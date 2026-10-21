using Godot;
using GrimSpace.World.StarSystem.Presentation.Ui;

namespace GrimSpace.Presentation.Dev.Hud;

public sealed partial class FleetManagementHudPreview : Control
{
	public override void _Ready()
	{
		UiHudPreviewChrome.Attach(
			this,
			"Fleet management HUD preview",
			"Starter run fleet · carousel and pilot popup");

		var dev = UiHudDevContext.Create();
		var overlay = new FleetManagementOverlay();
		UiHudPreviewChrome.MountOverlay(this, overlay);
		overlay.Open(dev.Run);
	}
}
