using Godot;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Presentation.Ui;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.Presentation.Dev.Hud;

public sealed partial class EngagementHudPreview : Control
{
	[Export]
	public bool IncludeAssignedContract { get; set; } = true;

	public override void _Ready()
	{
		UiHudPreviewChrome.Attach(
			this,
			"Engagement HUD preview",
			IncludeAssignedContract ? "With hunt contract context" : "Hostile contact only");

		var dev = UiHudDevContext.Create();
		var overlay = new EngagementHudOverlay();
		UiHudPreviewChrome.MountOverlay(this, overlay);
		overlay.Sync(new PendingEngagement(
			"preview-pirate-fleet",
			EType.PirateFleet,
			EFaction.Pirates,
			"2 × Repurposed Miner (T1)\n1 × Fighter (T0)",
			IncludeAssignedContract ? dev.SampleHuntContract() : null,
			FleeFailsDelivery: false));
	}
}
