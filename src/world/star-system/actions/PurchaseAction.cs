using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record PurchaseAction(
	string ActorId,
	MerchantEndpoint Endpoint,
	EMerchantCatalog Catalog,
	MerchantCatalog.Offering Offering,
	MerchantCatalog.Target Target,
	ShipInstance Before,
	ResourceBundle QuotedCost) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		PurchaseActionDef.Instance;
}

public sealed class PurchaseActionDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static PurchaseActionDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime)
	{
		if (action is not PurchaseAction purchase)
			return false;
		if (!TryResolveQuotedOffer(purchase, world, runtime, out var offer))
			return false;
		if (!MerchantShipChanges.TryPrepareAfter(
				purchase.Offering,
				purchase.Target,
				purchase.Before,
				out _))
			return false;

		return world.PlayerResources.CanApply(offer.Cost.Negate());
	}

	internal static bool EndpointServesPurchaseProvider(PurchaseAction purchase, StarMap world) =>
		FetchMerchantCatalogActionDef.EndpointServesCatalog(
			purchase.Endpoint,
			purchase.Catalog,
			world);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var purchase = (PurchaseAction)action;
		if (!TryResolveQuotedOffer(purchase, world, runtime, out var offer)
			|| !MerchantShipChanges.TryPrepareAfter(
				purchase.Offering,
				purchase.Target,
				purchase.Before,
				out var after))
			return [];

		var fact = new MerchantShipPurchase(
			purchase.Before.Id,
			purchase.Before.Clone(),
			after.Clone(),
			offer.Cost);

		return
		[
			new ChangeResourceEffect(TransactionSource.MerchantPurchase, offer.Cost.Negate()),
			new RecordMerchantShipPurchaseEffect(fact),
		];
	}

	internal static bool TryResolveQuotedOffer(
		PurchaseAction purchase,
		StarMap world,
		ActorRuntime runtime,
		out MerchantCatalog.Offer offer)
	{
		offer = default!;
		if (!world.FleetRegistry.TryGet(purchase.ActorId, out var fleet))
			return false;
		if (!fleet.Members.Any(member =>
				string.Equals(member.Id, purchase.Before.Id, StringComparison.Ordinal)))
			return false;
		if (!EndpointServesPurchaseProvider(purchase, world))
			return false;
		if (world.ItemInventory?.Matches(purchase.Before.Id, purchase.Before) != true)
			return false;
		var pricingAction = FetchMerchantCatalogActionDef.CreatePricingProbe(
			purchase.ActorId,
			purchase.Endpoint,
			purchase.Catalog,
			purchase.Before);
		offer = FetchMerchantCatalogActionDef.PriceCatalog(
				pricingAction,
				world,
				runtime)
			.Quote
			.Offers
			.FirstOrDefault(candidate =>
				candidate.Offering == purchase.Offering
				&& candidate.Targets.Contains(purchase.Target))!;
		if (offer is null)
			return false;

		return offer.Cost.Equals(purchase.QuotedCost);
	}
}
