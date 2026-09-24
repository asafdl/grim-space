using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record PurchaseWeaponsUpgradeAction(
	string ActorId,
	string PoiId,
	string FacilityId,
	string OperatorName,
	string OfferId,
	ShipInstance Before) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		PurchaseWeaponsUpgradeDef.Instance;
}

public sealed class PurchaseWeaponsUpgradeDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static PurchaseWeaponsUpgradeDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is PurchaseWeaponsUpgradeAction purchase
		&& world.FleetRegistry.TryGet(purchase.ActorId, out _)
		&& MerchantPurchaseValidation.OperatorServesCatalog(
			world,
			purchase.PoiId,
			purchase.FacilityId,
			purchase.OperatorName,
			EMerchantCatalog.Weapons)
		&& TryResolvePurchase(purchase.OfferId, purchase.Before, out _, out var cost)
		&& world.PlayerResources.CanApply(cost.Negate());

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var purchase = (PurchaseWeaponsUpgradeAction)action;
		if (!TryResolvePurchase(purchase.OfferId, purchase.Before, out var after, out var cost))
			return [];

		var fact = new MerchantShipPurchase(
			purchase.Before.Id,
			purchase.Before.Clone(),
			after.Clone(),
			cost,
			purchase.OfferId);

		return
		[
			new ChangeResourceEffect(TransactionSource.MerchantPurchase, cost.Negate()),
			new RecordMerchantShipPurchaseEffect(fact),
		];
	}

	private static bool TryResolvePurchase(
		string offerId,
		ShipInstance before,
		out ShipInstance after,
		out ResourceBundle cost)
	{
		after = null!;
		cost = ResourceBundle.Empty;
		if (!WeaponsCatalog.TryQuote(offerId, before, out cost)
			|| !WeaponsCatalog.TryGetOffer(offerId, before, out var offer))
			return false;

		return offer.Category switch
		{
			EWeaponsOfferCategory.MaxShields => before.TryWithUpgradedMaxShields(out after),
			EWeaponsOfferCategory.Ability => offer.Mount is { } mount
				&& before.TryWithUpgradedAbility(mount, out after),
			_ => false,
		};
	}
}
