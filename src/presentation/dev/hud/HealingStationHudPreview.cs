using Godot;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.World.StarSystem.Presentation.Facilities;

namespace GrimSpace.Presentation.Dev.Hud;

public sealed partial class HealingStationHudPreview : Control
{
	public override void _Ready()
	{
		UiHudPreviewChrome.Attach(
			this,
			"Healing station HUD preview",
			"Trade hub healing catalog");

		var dev = UiHudDevContext.Create();
		var endpoint = dev.TradeHubEndpoint(
			TradeHub.DockyardFacilitySlug,
			TradeHub.DockyardHealingStationSceneSlotId);
		var overlay = new HealingStationHudOverlay();
		UiHudPreviewChrome.MountOverlay(this, overlay);
		overlay.Open(
			dev.Run,
			dev.Map,
			"Preview healing bay",
			dev.Intents,
			endpoint);
		overlay.ReceiveCatalogQuote(
			dev.BuildCatalogQuote(endpoint, EMerchantCatalog.Healing));
	}
}
