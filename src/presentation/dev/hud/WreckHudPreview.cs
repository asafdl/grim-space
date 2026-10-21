using Godot;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Presentation.Ui;

namespace GrimSpace.Presentation.Dev.Hud;

public sealed partial class WreckHudPreview : Control
{
	[Export]
	public bool Ambush { get; set; }

	public override void _Ready()
	{
		UiHudPreviewChrome.Attach(
			this,
			"Wreck decision HUD preview",
			Ambush ? "Ambush briefing variant" : "Salvage briefing variant");

		var overlay = new WreckHudOverlay();
		UiHudPreviewChrome.MountOverlay(this, overlay);
		overlay.Sync(new PendingWreckDecision(
			"preview-wreck-contract",
			"Derelict courier",
			Ambush
				? "Scanners show a stripped hull drifting inside the debris field. Multiple hostile transponders are masked behind the wreck signature."
				: "A lightly damaged courier rests in the debris field. Salvage teams estimate recoverable cargo and intact components.",
			Ambush));
	}
}
