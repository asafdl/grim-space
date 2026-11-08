using Godot;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Components;
using GrimSpace.Run;
using GrimSpace.Units.Enums;
using GrimSpace.Ui.Components;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Presentation.Ui;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

internal static class ShipOfferCardList
{
	public static VBoxContainer Build(State run, Action<RecruitmentOffer> onPurchase)
	{
		var body = HudWidgets.CreateCardList();
		body.AddThemeConstantOverride("separation", 10);

		var offers = run.StarSystem.Map.Market.ListRecruitmentOffers();
		if (offers.Count == 0)
			body.AddChild(HudWidgets.CreateStatusPanel(
				HudStatusKind.Neutral,
				"No ships are available."));

		foreach (var offer in offers)
		{
			var canAfford = run.StarSystem.Map.PlayerResources.CanApply(offer.Cost.Negate());
			var item = BuildOfferCard(offer, canAfford, onPurchase);
			item.SetTooltip(ShipRecruitmentDisplay.OfferTooltip(offer));
			body.AddChild(item);
		}

		return body;
	}

	private static UiListItem BuildOfferCard(
		RecruitmentOffer offer,
		bool canAfford,
		Action<RecruitmentOffer> onPurchase)
	{
		var title = PilotHireDisplay.ShipChassisLabel(offer.Chassis);
		void Request() => onPurchase(offer);

		var portrait = UnitPortraitCatalog.For(offer.Chassis)
			?? throw new InvalidOperationException($"No portrait for ship chassis {offer.Chassis}.");
		var item = new UiImageListItem(
			title,
			"",
			"",
			portrait,
			frameEdgeColor: TierFrameColor(offer.GearTier))
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		item.SetDetailContent(ResourceCostDisplay.CreateOfferDetails(
			offer.GearTier.ToString(),
			offer.Cost));
		item.SetAffordable(canAfford);
		item.SetEnabled(canAfford);
		if (canAfford)
			item.Pressed += Request;
		return item;
	}

	private static Color? TierFrameColor(EShipGearTier tier) =>
		ShipGearTierTint.TryGet(tier, out var tint) ? tint : null;
}
