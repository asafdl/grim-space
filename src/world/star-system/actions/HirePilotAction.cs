using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Ids;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record HirePilotAction(
	string ActorId,
	LeadSaleMerchantEndpoint Endpoint,
	int Slot) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		HirePilotActionDef.Instance;
}

public sealed class HirePilotActionDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static HirePilotActionDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime)
	{
		if (action is not HirePilotAction hire)
			return false;
		if (!TryResolveHire(hire, world, out var contractId, out var candidate, out var cost, out _))
			return false;
		if (!MerchantPurchaseValidation.EndpointServesMerchantSubtype(
				world,
				hire.Endpoint,
				EMerchantSubtype.Pilots))
			return false;
		if (!world.FleetRegistry.TryGet(hire.ActorId, out var fleet))
			return false;
		if (fleet.State.CurrentEngagement?.Phase == EEngagementPhase.Engaged)
			return false;

		return world.PlayerResources.CanApply(cost.Negate());
	}

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		if (action is not HirePilotAction hire)
			return [];
		if (!TryResolveHire(hire, world, out var contractId, out var candidate, out var cost, out var shipId))
			return [];

		var ship = new ShipSpawnDeclaration(shipId, candidate.Ship.Chassis, candidate.Ship.GearTier);
		var committed = new PilotHireCommitted(candidate.Profile, ship);

		return
		[
			new ChangeResourceEffect(TransactionSource.MerchantPurchase, cost.Negate()),
			new MarkLeadPilotHireSlotPurchasedEffect(contractId, hire.Slot),
			new PilotHireCommittedEffect(committed),
		];
	}

	internal static bool TryResolveHire(
		HirePilotAction hire,
		StarMap world,
		out string contractId,
		out PilotRecruitment.PilotHireCandidate candidate,
		out ResourceBundle cost,
		out string shipId)
	{
		contractId = hire.Endpoint.ContractId;
		candidate = null!;
		cost = ResourceBundle.Empty;
		shipId = "";
		if (string.IsNullOrWhiteSpace(contractId))
			return false;
		if (!world.ContractRegistry.TryGetState(contractId, out var state)
			|| state is not LeadContractState lead)
			return false;
		if (lead.PurchasedPilotHireSlots.Contains(hire.Slot))
			return false;
		if (!PilotRecruitment.TryGetHireCandidate(world, contractId, hire.Slot, out candidate))
			return false;

		cost = PilotRecruitment.Price(world, candidate);
		shipId = TypedIdGenerator.NextId(UnitTypeSlug.For(candidate.Ship.Chassis));
		if (world.ItemInventory?.TryGet(shipId, out _) == true)
			return false;

		return true;
	}
}
