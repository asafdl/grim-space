using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

[Obsolete("Retained only to deserialize timelines from saves created before contract visits became ephemeral.")]
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

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => false;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) => false;

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
		=> throw new NotSupportedException(
			"Contract operator visits are ephemeral and cannot be committed as actions.");
}
