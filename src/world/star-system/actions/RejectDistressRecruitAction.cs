using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

/// <summary>Closes the prompt without registering the survivor.</summary>
public sealed record RejectDistressRecruitAction(
	string ActorId,
	string PendingId) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		RejectDistressRecruitDef.Instance;
}

public sealed class RejectDistressRecruitDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static RejectDistressRecruitDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is RejectDistressRecruitAction reject
		&& world.WaitingForPlayerInput
		&& world.PendingWorldEvent is { EventId: EWorldEventId.DistressCall, Reward: null } pending
		&& pending.Recruit is not null
		&& string.Equals(pending.Id, reject.PendingId, StringComparison.Ordinal)
		&& world.FleetRegistry.TryGet(reject.ActorId, out _);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		if (!IsLegal(action, world, runtime))
			return [];

		return
		[
			new SetPendingWorldEventEffect(null),
			new PlayerInputEffect(false),
		];
	}
}
