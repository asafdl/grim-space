using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

/// <summary>Pays the demanded stake once and releases the player-input wait. The pilot stays.</summary>
public sealed record PayCompulsiveStakeAction(
	string ActorId,
	string PendingId) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		PayCompulsiveStakeDef.Instance;
}

public sealed class PayCompulsiveStakeDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static PayCompulsiveStakeDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is PayCompulsiveStakeAction pay
		&& world.WaitingForPlayerInput
		&& world.PendingWorldEvent is { } pending
		&& CompulsiveStakeDemandedDef.IsStake(pending)
		&& string.Equals(pending.Id, pay.PendingId, StringComparison.Ordinal)
		&& world.PlayerResources.CanApply(pending.Reward!)
		&& world.FleetRegistry.TryGet(pay.ActorId, out _);

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
			new ChangeResourceEffect(TransactionSource.WorldEvent, pending.Reward!),
			new SetPendingWorldEventEffect(null),
			new PlayerInputEffect(false),
		];
	}
}
