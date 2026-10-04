using GrimSpace.Core.Actions;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record GrantDebugResourcesAction(
	string ActorId,
	ResourceBundle Resources) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		GrantDebugResourcesActionDef.Instance;
}

public sealed class GrantDebugResourcesActionDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static GrantDebugResourcesActionDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is GrantDebugResourcesAction grant
		&& !grant.Resources.IsEmpty
		&& world.PlayerResources.CanApply(grant.Resources);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime) =>
		[new ChangeResourceEffect(TransactionSource.DebugGrant, ((GrantDebugResourcesAction)action).Resources)];
}
