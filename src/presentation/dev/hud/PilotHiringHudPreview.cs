using Godot;
using GrimSpace.World.StarSystem.Presentation.Facilities;

namespace GrimSpace.Presentation.Dev.Hud;

public sealed partial class PilotHiringHudPreview : Control
{
	public override void _Ready()
	{
		UiHudPreviewChrome.Attach(
			this,
			"Pilot hiring HUD preview",
			"Accepted black-market pilot lead · hire cards");

		var dev = UiHudDevContext.Create();
		var contractId = dev.PrepareAcceptedPilotHireLead();
		var overlay = new PilotHiringHudOverlay();
		UiHudPreviewChrome.MountOverlay(this, overlay);
		overlay.Open(dev.Run, "Preview broker", contractId);
	}
}
