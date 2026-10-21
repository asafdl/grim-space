using Godot;
using GrimSpace.World.StarSystem.Narrative;
using GrimSpace.World.StarSystem.Presentation.Ui;

namespace GrimSpace.Presentation.Dev.Hud;

public sealed partial class NarrativeHudPreview : Control
{
	public override void _Ready()
	{
		UiHudPreviewChrome.Attach(
			this,
			"Narrative HUD preview",
			"Multi-page typewriter flow");

		var overlay = new NarrativeHudOverlay();
		AddChild(overlay);
		overlay.Open(new NarrativeDefinition(
			"preview-narrative",
			[
				"[b]Sector brief[/b]\n\nLocal traffic is thin. The trade hub still holds contracts, but pirate sightings are up along the outer lanes.",
				"Your fleet is cleared for independent operations. Review contracts at command authority when you are ready to take work.",
			]));
	}
}
