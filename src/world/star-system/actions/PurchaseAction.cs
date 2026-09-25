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
	string PoiId,
	string FacilityId,
	string OperatorName,
	EMerchantCatalog Catalog,
	MerchantCatalog.Offering Offering,
	ShipInstance Before) : IAction<StarMap, ActorRuntime>
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
		if (!world.FleetRegistry.TryGet(purchase.ActorId, out var fleet))
			return false;
		if (!fleet.Members.Any(member =>
				string.Equals(member.Id, purchase.Before.Id, StringComparison.Ordinal)))
			return false;
		if (!MerchantPurchaseValidation.OperatorServesCatalog(
				world,
				purchase.PoiId,
				purchase.FacilityId,
				purchase.OperatorName,
				purchase.Catalog))
			return false;
		if (world.ShipRegistryReader?.Matches(purchase.Before.Id, purchase.Before) != true)
			return false;
		if (!MerchantCatalog.TryFind(purchase.Catalog, purchase.Offering, purchase.Before, out var offer))
			return false;
		if (!MerchantShipChanges.TryPrepareAfter(purchase.Offering, purchase.Before, out _))
			return false;

		return world.PlayerResources.CanApply(offer.Cost.Negate());
	}

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var purchase = (PurchaseAction)action;
		if (!MerchantCatalog.TryFind(purchase.Catalog, purchase.Offering, purchase.Before, out var offer)
			|| !MerchantShipChanges.TryPrepareAfter(purchase.Offering, purchase.Before, out var after))
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
}
