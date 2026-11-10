using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

/// <summary>Player acknowledgment: applies the cruiser's return fire once and releases the wait.</summary>
public sealed record AcknowledgePotshotAction(
	string ActorId,
	string PendingId) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		AcknowledgePotshotDef.Instance;
}

public sealed class AcknowledgePotshotDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static AcknowledgePotshotDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is AcknowledgePotshotAction ack
		&& world.WaitingForPlayerInput
		&& world.PendingWorldEvent is { EventId: EWorldEventId.Potshot } pending
		&& string.Equals(pending.Id, ack.PendingId, StringComparison.Ordinal)
		&& world.FleetRegistry.TryGet(ack.ActorId, out _);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		if (!IsLegal(action, world, runtime))
			return [];

		var pending = world.PendingWorldEvent!;
		var effects = new List<IEffect<StarMap, ActorRuntime>>(3);
		if (pending.ShipId is not null && world.ItemInventory?.TryGet(pending.ShipId, out var ship) == true)
		{
			var before = ship.Clone();
			var after = FieldedShipDamage.Apply(before, pending.Damage, minimumHull: 1);
			effects.Add(new RecordFieldedShipDamageEffect(
				new FieldedShipDamage(
					"world event",
					[new FieldedShipChange(before.Id, before, after)])));
		}

		effects.Add(new SetPendingWorldEventEffect(null));
		effects.Add(new PlayerInputEffect(false));
		return effects;
	}
}
