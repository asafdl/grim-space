using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record DistressCallReceivedAction(
	string ActorId,
	PendingWorldEvent Pending) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		DistressCallReceivedDef.Instance;
}

public sealed class DistressCallReceivedDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static DistressCallReceivedDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is DistressCallReceivedAction received
		&& string.Equals(received.ActorId, StarSystemActorIds.WorldEvents, StringComparison.Ordinal)
		&& received.Pending.EventId == EWorldEventId.DistressCall
		&& HasExactlyOnePayload(received.Pending)
		&& WorldEventContexts.CanOpenPrompt(world);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime) =>
		[
			new SetPendingWorldEventEffect(((DistressCallReceivedAction)action).Pending),
			new PlayerInputEffect(true),
		];

	private static bool HasExactlyOnePayload(PendingWorldEvent pending) =>
		pending.Reward is { IsEmpty: false } != pending.Recruit is not null;
}
