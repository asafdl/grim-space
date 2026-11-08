using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

/// <summary>Player acknowledgment: applies the asteroid damage once and releases the player-input wait.</summary>
public sealed record AcknowledgeAsteroidImpactAction(
	string ActorId,
	string PendingId) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		AcknowledgeAsteroidImpactDef.Instance;
}

public sealed class AcknowledgeAsteroidImpactDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static AcknowledgeAsteroidImpactDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is AcknowledgeAsteroidImpactAction ack
		&& world.WaitingForPlayerInput
		&& world.PendingWorldEvent is { EventId: EWorldEventId.AsteroidAccident } pending
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
		if (world.ItemInventory?.TryGet(pending.ShipId, out var ship) == true)
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
