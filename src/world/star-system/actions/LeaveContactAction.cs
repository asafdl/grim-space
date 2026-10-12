using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record LeaveContactAction(
	string ActorId,
	string TargetId,
	EContactIntent Intent) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		LeaveContactDef.Instance;
}

public sealed class LeaveContactDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static LeaveContactDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is LeaveContactAction leave
		&& world.WaitingForPlayerInput
		&& leave.Intent is EContactIntent.DeliveryMeeting or EContactIntent.BlackMarketMeeting
		&& world.FleetRegistry.TryGet(leave.ActorId, out _)
		&& world.FleetRegistry.TryGet(leave.TargetId, out var target)
		&& ReachContactDef.IsActiveMeeting(world, leave.ActorId, target, leave.Intent);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime) =>
		IsLegal(action, world, runtime)
			? [new PlayerInputEffect(false)]
			: [];
}
