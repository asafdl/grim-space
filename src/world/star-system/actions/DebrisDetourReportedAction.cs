using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record DebrisDetourReportedAction(
	string ActorId,
	PendingWorldEvent Pending) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		DebrisDetourReportedDef.Instance;
}

public sealed class DebrisDetourReportedDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static DebrisDetourReportedDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is DebrisDetourReportedAction reported
		&& string.Equals(reported.ActorId, StarSystemActorIds.WorldEvents, StringComparison.Ordinal)
		&& reported.Pending.EventId == EWorldEventId.DebrisDetour
		&& reported.Pending.Reward is { IsEmpty: false }
		&& WorldEventContexts.CanOpenPrompt(world);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime) =>
		[
			new SetPendingWorldEventEffect(((DebrisDetourReportedAction)action).Pending),
			new PlayerInputEffect(true),
		];
}
