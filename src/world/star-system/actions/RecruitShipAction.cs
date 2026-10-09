using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record RecruitShipAction(
	string ActorId,
	string PoiId,
	string FacilityId,
	string OperatorName,
	ShipSpawnDeclaration Declaration) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		RecruitShipActionDef.Instance;
}

public sealed class RecruitShipActionDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static RecruitShipActionDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime)
	{
		if (action is not RecruitShipAction recruit)
			return false;
		if (!MerchantPurchaseValidation.OperatorServesCatalog(
				world,
				recruit.PoiId,
				recruit.FacilityId,
				recruit.OperatorName,
				EMerchantCatalog.Ships))
			return false;
		if (!ShipRecruitmentCatalog.TryFind(
				recruit.Declaration.Chassis,
				recruit.Declaration.GearTier,
				out var offer))
			return false;
		if (!EnlistPlayerShipActionDef.Instance.IsLegal(
				new EnlistPlayerShipAction(recruit.ActorId, recruit.Declaration),
				world,
				runtime))
			return false;

		return world.PlayerResources.CanApply(offer.Cost.Negate());
	}

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var recruit = (RecruitShipAction)action;
		if (!ShipRecruitmentCatalog.TryFind(
				recruit.Declaration.Chassis,
				recruit.Declaration.GearTier,
				out var offer))
			return [];

		return
		[
			new ChangeResourceEffect(TransactionSource.MerchantPurchase, offer.Cost.Negate()),
			new EnlistPlayerShipEffect(recruit.Declaration),
		];
	}
}
