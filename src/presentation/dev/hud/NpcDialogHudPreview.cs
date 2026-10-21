using Godot;
using GrimSpace.World.StarSystem.Poi.Dialog;
using GrimSpace.World.StarSystem.Presentation.Facilities;

namespace GrimSpace.Presentation.Dev.Hud;

public sealed partial class NpcDialogHudPreview : Control
{
	public override void _Ready()
	{
		UiHudPreviewChrome.Attach(
			this,
			"NPC dialog HUD preview",
			"Black-market lead conversation");

		var overlay = new NpcDialogHudOverlay();
		AddChild(overlay);
		overlay.Open(FacilityNpcDialogs.BlackMarketLead(
			"Quiet broker",
			"I hear you're looking for crew. I can arrange introductions—for a fee, paid later at the meet."));
	}
}
