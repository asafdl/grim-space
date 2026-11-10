using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record PotshotReportedAction(
	string ActorId,
	PendingWorldEvent Pending) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		PotshotReportedDef.Instance;
}

public sealed class PotshotReportedDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static PotshotReportedDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is PotshotReportedAction reported
		&& string.Equals(reported.ActorId, StarSystemActorIds.WorldEvents, StringComparison.Ordinal)
		&& reported.Pending.EventId == EWorldEventId.Potshot
		&& reported.Pending.Damage > 0
		&& WorldEventContexts.CanOpenPrompt(world);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime) =>
		[
			new SetPendingWorldEventEffect(((PotshotReportedAction)action).Pending),
			new PlayerInputEffect(true),
		];
}
