using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Economy;
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
		if (!IsOffered(purchase, world))
			return false;
		if (!MerchantShipChanges.TryPrepareAfter(
				purchase.Offering,
				purchase.Target,
				purchase.Before,
				out _))
			return false;

		return world.PlayerResources.CanApply(purchase.QuotedCost.Negate());
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
		if (!IsOffered(purchase, world)
			|| !MerchantShipChanges.TryPrepareAfter(
				purchase.Offering,
				purchase.Target,
				purchase.Before,
				out var after))
			return [];

		if (purchase.Offering.Kind == MerchantCatalog.Kind.BuyFuel)
		{
			var change = new Dictionary<ResourceId, int>();
			foreach (var (id, amount) in purchase.QuotedCost.Negate())
				change[id] = amount;
			change[ResourceId.Fuel] = change.GetValueOrDefault(ResourceId.Fuel)
				+ StrategicEconomyConfig.FuelPurchaseAmount;
			return
			[
				new ChangeResourceEffect(
					TransactionSource.FuelPurchase,
					ResourceBundle.Create(change)),
			];
		}

		var fact = new MerchantShipPurchase(
			purchase.Before.Id,
			purchase.Before.Clone(),
			after.Clone(),
			purchase.QuotedCost);

		return
		[
			new ChangeResourceEffect(TransactionSource.MerchantPurchase, purchase.QuotedCost.Negate()),
			new RecordMerchantShipPurchaseEffect(fact),
		];
	}

	internal static bool IsOffered(PurchaseAction purchase, StarMap world) =>
		world.ItemInventory?.IsOwnedBy(purchase.Before.Id, purchase.ActorId) == true
		&& EndpointServesPurchaseProvider(purchase, world)
		&& world.ItemInventory.Matches(purchase.Before.Id, purchase.Before)
		&& MerchantCatalog.ListFor(purchase.Catalog, purchase.Before, world).Any(offer =>
			offer.Offering == purchase.Offering && offer.Target == purchase.Target);
}
