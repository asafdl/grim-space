using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

/// <summary>Ignores the stake. The pilot might walk. The wait releases either way.</summary>
public sealed record RefuseCompulsiveStakeAction(
	string ActorId,
	string PendingId) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		RefuseCompulsiveStakeDef.Instance;
}

public sealed class RefuseCompulsiveStakeDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static RefuseCompulsiveStakeDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is RefuseCompulsiveStakeAction refuse
		&& world.WaitingForPlayerInput
		&& world.PendingWorldEvent is { } pending
		&& CompulsiveStakeDemandedDef.IsStake(pending)
		&& string.Equals(pending.Id, refuse.PendingId, StringComparison.Ordinal)
		&& world.PilotRegistry?.TryGet(pending.PilotId, out _) == true
		&& world.FleetRegistry.TryGet(refuse.ActorId, out _);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		if (!IsLegal(action, world, runtime))
			return [];

		var pending = world.PendingWorldEvent!;
		var effects = new List<IEffect<StarMap, ActorRuntime>>();
		if (RollForCompulsiveStakeDef.WalksOnIgnore(pending.Id))
			effects.Add(new PilotWalkedOutEffect(pending.PilotId));
		effects.Add(new SetPendingWorldEventEffect(null));
		effects.Add(new PlayerInputEffect(false));
		return effects;
	}
}
