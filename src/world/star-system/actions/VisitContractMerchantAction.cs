using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contracts.Generation;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record VisitContractMerchantAction(
	string ActorId,
	string PoiId,
	string FacilityId,
	string OperatorName) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		VisitContractMerchantDef.Instance;
}

public sealed class VisitContractMerchantDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static VisitContractMerchantDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is VisitContractMerchantAction visit
		&& world.FleetRegistry.TryGet(visit.ActorId, out _)
		&& ResolvesToContractMerchant(world, visit);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var visit = (VisitContractMerchantAction)action;
		return
		[
			new PauseContractIssuerGenerationEffect(
				visit.PoiId,
				world.Timeline.Clock.Current
				+ ContractBoardConfig.DefaultMerchantRefreshCooldownTicks),
		];
	}

	private static bool ResolvesToContractMerchant(
		StarMap world,
		VisitContractMerchantAction visit)
	{
		if (!world.TryGetPointOfInterest(visit.PoiId, out var poi))
			return false;

		Facility facility;
		try
		{
			facility = poi.GetFacility(visit.FacilityId);
		}
		catch (InvalidOperationException)
		{
			return false;
		}

		var facilityOperator = facility.Operators.FirstOrDefault(candidate =>
			string.Equals(candidate.Name, visit.OperatorName, StringComparison.OrdinalIgnoreCase));
		if (facilityOperator is null)
			return false;

		var role = poi.ResolveInteractionRole(
			facility.Id,
			facilityOperator.Name,
			facilityOperator.Role);
		return role == EFacilityOperatorRole.Contracts
			|| world.ContractRegistry.AvailableForPoi(visit.PoiId).Any();
	}
}
