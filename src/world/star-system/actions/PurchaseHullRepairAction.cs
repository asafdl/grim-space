using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Dockyard;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record PurchaseHullRepairAction(
	string ActorId,
	string PoiId,
	string FacilityId,
	ShipInstance Before) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		PurchaseHullRepairDef.Instance;
}

public sealed class PurchaseHullRepairDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static PurchaseHullRepairDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is PurchaseHullRepairAction purchase
		&& world.FleetRegistry.TryGet(purchase.ActorId, out _)
		&& TryResolveFacility(purchase, world, out _)
		&& TryResolvePurchase(purchase.Before, out _, out var cost)
		&& world.PlayerResources.CanApply(cost.Negate());

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var purchase = (PurchaseHullRepairAction)action;
		if (!TryResolvePurchase(purchase.Before, out var after, out var cost))
			return [];

		var fact = new HullRepairPurchased(
			purchase.Before.Id,
			purchase.Before.Clone(),
			after.Clone(),
			cost);

		return
		[
			new ChangeResourceEffect(TransactionSource.DockyardPurchase, cost.Negate()),
			new RecordHullRepairEffect(fact),
		];
	}

	internal static bool TryResolveFacility(
		PurchaseHullRepairAction purchase,
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

	private static bool TryResolvePurchase(
		ShipInstance before,
		out ShipInstance after,
		out ResourceBundle cost)
	{
		cost = ResourceBundle.Empty;
		return DockyardHullRepair.TryApply(before, out after)
			&& DockyardHullRepair.TryQuote(before, out cost);
	}
}
