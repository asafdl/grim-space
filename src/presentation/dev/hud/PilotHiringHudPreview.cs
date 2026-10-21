using Godot;
using GrimSpace.World.StarSystem.Presentation.Facilities;

namespace GrimSpace.Presentation.Dev.Hud;

public sealed partial class PilotHiringHudPreview : Control
{
	public override void _Ready()
	{
		var dev = UiHudDevContext.Create();
		var setup = dev.PreparePilotHirePreview();

		UiHudPreviewChrome.Attach(
			this,
			"Pilot hiring HUD preview",
			setup.PresentationTitle);

		var overlay = new PilotHiringHudOverlay();
		UiHudPreviewChrome.MountOverlay(this, overlay);
		overlay.Open(dev.Run, setup.PresentationTitle, setup.ContractId);
		overlay.HireRequested += slot =>
		{
			dev.Intents.TryHirePilot(setup.Endpoint, slot);
			dev.StarSystem.RefreshPlayerAgent();
			overlay.Sync(dev.Run, setup.ContractId);
		};
	}
}
