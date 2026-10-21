using Godot;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.World.StarSystem.Presentation.Facilities;

namespace GrimSpace.Presentation.Dev.Hud;

public sealed partial class DockyardHudPreview : Control
{
	[Export]
	public EMerchantSubtype Subtype { get; set; } = EMerchantSubtype.ShipSupport;

	public override void _Ready()
	{
		UiHudPreviewChrome.Attach(
			this,
			$"Dockyard HUD preview ({Subtype})",
			"Catalog primed from generated run state");

		var dev = UiHudDevContext.Create();
		var (facilitySlug, slotId, catalog) = Subtype switch
		{
			EMerchantSubtype.Weapons => (
				TradeHub.DockyardFacilitySlug,
				TradeHub.ShopOperatorSceneSlotId,
				EMerchantCatalog.Weapons),
			EMerchantSubtype.ShipSupport => (
				TradeHub.DockyardFacilitySlug,
				TradeHub.ShieldOperatorSceneSlotId,
				EMerchantCatalog.ShipSupport),
			_ => throw new InvalidOperationException($"Dockyard preview does not support '{Subtype}'."),
		};

		var endpoint = dev.TradeHubEndpoint(facilitySlug, slotId);
		var overlay = new DockyardHudOverlay();
		UiHudPreviewChrome.MountOverlay(this, overlay);
		overlay.Open(
			dev.Run,
			dev.Map,
			"Preview dockyard",
			Subtype,
			dev.Intents,
			endpoint,
			MerchantSubtypeRouting.CatalogFor(Subtype));
		overlay.ReceiveCatalogQuote(dev.BuildCatalogQuote(endpoint, catalog));
	}
}
