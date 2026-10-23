using GrimSpace.Core.Actions;
using GrimSpace.Pilots;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.PilotCapabilities;

public sealed class ShipUpgradeDiscountModifier(int stackCount)
	: IActionEffectModifier<StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	private static int UpgradeDiscountPercentPerStack =>
		PilotSkillPercents.IKnowABot.ShipUpgradeDiscountPercent;

	public IEffect<StarMap, ActorRuntime> Modify(
		IAction action,
		StarMap world,
		ActorRuntime runtime,
		IEffect<StarMap, ActorRuntime> effect)
	{
		if (stackCount == 0
			|| effect is not PublishMerchantCatalogEffect publish)
			return effect;

		var quote = publish.Quote;
		var discountPercent = UpgradeDiscountPercentPerStack * stackCount;
		var offers = quote.Offers
			.Select(offer => offer with
			{
				Cost = IsUpgrade(offer.Offering.Kind)
					? ApplyUpgradeDiscount(offer.Cost, discountPercent)
					: offer.Cost,
			})
			.ToArray();
		return new PublishMerchantCatalogEffect(quote with { Offers = offers });
	}

	private static bool IsUpgrade(MerchantCatalog.Kind kind) =>
		kind is MerchantCatalog.Kind.UpgradeDamage
			or MerchantCatalog.Kind.UpgradeRange
			or MerchantCatalog.Kind.UpgradeMaxShields;

	private static ResourceBundle ApplyUpgradeDiscount(ResourceBundle cost, int discountPercent)
	{
		if (cost.IsEmpty)
			return cost;

		var remainingPercent = System.Math.Max(0, 100 - discountPercent);
		return ResourceBundle.Create(cost.ToDictionary(
			entry => entry.Key,
			entry => entry.Key is ResourceId.Credits or ResourceId.ScrapAlloy
				? (int)decimal.Ceiling(entry.Value * remainingPercent / 100m)
				: entry.Value));
	}
}
