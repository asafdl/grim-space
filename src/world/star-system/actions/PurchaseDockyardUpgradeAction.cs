using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Dockyard;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record PurchaseDockyardUpgradeAction(
	string ActorId,
	string PoiId,
	string FacilityId,
	string OfferId,
	ShipInstance Before) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		PurchaseDockyardUpgradeDef.Instance;
}

public sealed class PurchaseDockyardUpgradeDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static PurchaseDockyardUpgradeDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is PurchaseDockyardUpgradeAction purchase
		&& world.FleetRegistry.TryGet(purchase.ActorId, out _)
		&& TryResolveFacility(purchase, world, out _)
		&& DockyardOffers.TryApply(purchase.OfferId, purchase.Before, out _)
		&& DockyardOffers.TryQuote(purchase.OfferId, purchase.Before, out var cost)
		&& world.PlayerResources.CanApply(cost.Negate());

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var purchase = (PurchaseDockyardUpgradeAction)action;
		if (!DockyardOffers.TryApply(purchase.OfferId, purchase.Before, out var after)
			|| !DockyardOffers.TryQuote(purchase.OfferId, purchase.Before, out var cost))
			return [];

		var fact = new DockyardUpgradePurchased(
			purchase.Before.Id,
			purchase.Before.Clone(),
			after.Clone(),
			purchase.OfferId,
			cost);

		return
		[
			new ChangeResourceEffect(TransactionSource.DockyardPurchase, cost.Negate()),
			new RecordDockyardUpgradeEffect(fact),
		];
	}

	internal static bool TryResolveFacility(
		PurchaseDockyardUpgradeAction purchase,
		StarMap world,
		out Facility facility)
	{
		facility = null!;
		var poi = world.PointsOfInterest.FirstOrDefault(candidate =>
			string.Equals(candidate.Id, purchase.PoiId, StringComparison.Ordinal));
		if (poi is null)
			return false;

		facility = poi.Facilities.FirstOrDefault(candidate =>
			string.Equals(candidate.Id, purchase.FacilityId, StringComparison.Ordinal))!;
		if (facility is null)
			return false;

		return facility.ServiceKinds.Contains(EServiceKind.Dockyard);
	}
}
