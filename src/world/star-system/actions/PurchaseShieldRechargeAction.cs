using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Dockyard;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record PurchaseShieldRechargeAction(
	string ActorId,
	string PoiId,
	string FacilityId,
	string OperatorName,
	ShipInstance Before,
	ESpatialOrientation? Face = null) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		PurchaseShieldRechargeDef.Instance;
}

public sealed class PurchaseShieldRechargeDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static PurchaseShieldRechargeDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is PurchaseShieldRechargeAction purchase
		&& world.FleetRegistry.TryGet(purchase.ActorId, out _)
		&& TryResolvePurchase(purchase.Before, purchase.Face, out _, out var cost)
		&& world.PlayerResources.CanApply(cost.Negate());

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var purchase = (PurchaseShieldRechargeAction)action;
		if (!TryResolvePurchase(purchase.Before, purchase.Face, out var after, out var cost))
			return [];

		var fact = new ShieldRechargePurchased(
			purchase.Before.Id,
			purchase.Before.Clone(),
			after.Clone(),
			cost);

		return
		[
			new ChangeResourceEffect(TransactionSource.DockyardPurchase, cost.Negate()),
			new RecordShieldRechargeEffect(fact),
		];
	}

	private static bool TryResolvePurchase(
		ShipInstance before,
		ESpatialOrientation? face,
		out ShipInstance after,
		out ResourceBundle cost)
	{
		after = null!;
		cost = ResourceBundle.Empty;
		if (face is { } target)
			return DockyardShieldRecharge.TryApplyFace(before, target, out after)
				&& DockyardShieldRecharge.TryQuoteFace(before, target, out cost);

		return DockyardShieldRecharge.TryApply(before, out after)
			&& DockyardShieldRecharge.TryQuote(before, out cost);
	}
}
