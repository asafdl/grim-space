using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

/// <summary>Player acknowledgment: grants the announced haul once and releases the player-input wait.</summary>
public sealed record AcknowledgeDriftSalvageAction(
	string ActorId,
	string PendingId) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		AcknowledgeDriftSalvageDef.Instance;
}

public sealed class AcknowledgeDriftSalvageDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static AcknowledgeDriftSalvageDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is AcknowledgeDriftSalvageAction ack
		&& world.WaitingForPlayerInput
		&& world.PendingWorldEvent is { EventId: EWorldEventId.DriftSalvage } pending
		&& string.Equals(pending.Id, ack.PendingId, StringComparison.Ordinal)
		&& pending.Reward is { IsEmpty: false } reward
		&& world.PlayerResources.CanApply(reward)
		&& world.FleetRegistry.TryGet(ack.ActorId, out _);

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
			new ChangeResourceEffect(TransactionSource.WorldEventSalvage, pending.Reward!),
			new SetPendingWorldEventEffect(null),
			new PlayerInputEffect(false),
		];
	}
}
