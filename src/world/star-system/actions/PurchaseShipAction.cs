using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record PurchaseShipAction(
	string ActorId,
	MerchantEndpoint Endpoint,
	ShipSpawnDeclaration Declaration) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		PurchaseShipActionDef.Instance;
}

public sealed class PurchaseShipActionDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static PurchaseShipActionDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime)
	{
		if (action is not PurchaseShipAction purchase)
			return false;
		if (!MerchantPurchaseValidation.EndpointServesMerchantSubtype(
				world,
				purchase.Endpoint,
				EMerchantSubtype.Ships))
			return false;
		if (!world.Market.TryFindRecruitmentOffer(
				purchase.Declaration.Chassis,
				purchase.Declaration.GearTier,
				MerchantCatalog.Tier(world),
				out var offer))
			return false;

		var shipId = purchase.Declaration.ShipId;
		if (string.IsNullOrWhiteSpace(shipId))
			return false;
		if (world.ItemInventory?.TryGet(shipId, out _) == true)
			return false;
		if (!world.FleetRegistry.TryGet(purchase.ActorId, out var fleet))
			return false;
		if (fleet.State.CurrentEngagement?.Phase == EEngagementPhase.Engaged)
			return false;

		return world.PlayerResources.CanApply(offer.Cost.Negate());
	}

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var purchase = (PurchaseShipAction)action;
		if (!world.Market.TryFindRecruitmentOffer(
				purchase.Declaration.Chassis,
				purchase.Declaration.GearTier,
				MerchantCatalog.Tier(world),
				out var offer))
			return [];

		return
		[
			new ChangeResourceEffect(TransactionSource.MerchantPurchase, offer.Cost.Negate()),
			new OwnedShipPurchasedEffect(purchase.Declaration),
		];
	}
}
