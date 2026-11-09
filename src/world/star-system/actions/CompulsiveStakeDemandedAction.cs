using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record CompulsiveStakeDemandedAction(
	string ActorId,
	PendingWorldEvent Pending) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		CompulsiveStakeDemandedDef.Instance;
}

public sealed class CompulsiveStakeDemandedDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static CompulsiveStakeDemandedDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is CompulsiveStakeDemandedAction demanded
		&& string.Equals(demanded.ActorId, StarSystemActorIds.WorldEvents, StringComparison.Ordinal)
		&& IsStake(demanded.Pending)
		&& WorldEventContexts.CanOpenPrompt(world);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime) =>
		[
			new SetPendingWorldEventEffect(((CompulsiveStakeDemandedAction)action).Pending),
			new PlayerInputEffect(true),
		];

	internal static bool IsStake(PendingWorldEvent pending) =>
		pending.EventId == EWorldEventId.CompulsiveStake
		&& pending.Recruit is null
		&& pending.Reward is { IsEmpty: false } reward
		&& reward.TryGet(ResourceId.Credits, out var credits)
		&& credits < 0;
}
