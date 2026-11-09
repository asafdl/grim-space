using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

/// <summary>Registers the stored survivor once and releases the player-input wait.</summary>
public sealed record AcceptDistressRecruitAction(
	string ActorId,
	string PendingId) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		AcceptDistressRecruitDef.Instance;
}

public sealed class AcceptDistressRecruitDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static AcceptDistressRecruitDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is AcceptDistressRecruitAction accept
		&& world.WaitingForPlayerInput
		&& world.PendingWorldEvent is { EventId: EWorldEventId.DistressCall, Reward: null } pending
		&& pending.Recruit is not null
		&& string.Equals(pending.Id, accept.PendingId, StringComparison.Ordinal)
		&& world.PilotRegistry is { CanRegister: true }
		&& world.FleetRegistry.TryGet(accept.ActorId, out _);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		if (!IsLegal(action, world, runtime))
			return [];

		var pending = world.PendingWorldEvent!;
		return
		[
			new PilotHireCommittedEffect(new PilotHireCommitted(pending.Recruit!)),
			new SetPendingWorldEventEffect(null),
			new PlayerInputEffect(false),
		];
	}
}
