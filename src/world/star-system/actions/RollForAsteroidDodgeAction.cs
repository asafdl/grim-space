using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record RollForAsteroidDodgeAction(
	string ActorId,
	ulong RngSeed) : IWorldEventRoll
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		RollForAsteroidDodgeDef.Instance;
}

public sealed class RollForAsteroidDodgeDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static RollForAsteroidDodgeDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is RollForAsteroidDodgeAction roll
		&& string.Equals(roll.ActorId, StarSystemActorIds.WorldEvents, StringComparison.Ordinal);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var roll = (RollForAsteroidDodgeAction)action;

		if (!WorldEventContexts.CanOpenPrompt(world)
			|| world.ItemInventory is null
			|| SelectCulprit(roll.RngSeed, WorldEventContexts.FieldedPilots(world)) is not { } culprit
			|| !WorldEventContexts.ShipIsTraveling(world, culprit.AssignedShipId!)
			|| !world.ItemInventory.TryGet(culprit.AssignedShipId!, out var ship))
			return [];

		var hit = FieldedShipDamage.Apply(ship, WorldEventCatalog.Asteroid.Damage, minimumHull: 1);
		var pending = new PendingWorldEvent(
			$"world-event-{roll.RngSeed:x}",
			EWorldEventId.AsteroidAccident,
			culprit.Id,
			culprit.AssignedShipId!,
			WorldEventCatalog.Asteroid.Damage,
			WorldEventCatalog.Asteroid.Announce(
				culprit.Name,
				ship.TotalCurrentShieldPoints - hit.TotalCurrentShieldPoints,
				ship.CurrentHullPoints - hit.CurrentHullPoints));
		return [new ScheduleWorldEventActionEffect(1, new AsteroidImpactAction(StarSystemActorIds.WorldEvents, pending))];
	}

	/// <summary>Checks pilots in shuffled order; the first to trigger is the culprit.</summary>
	public static Pilot? SelectCulprit(ulong rngSeed, IEnumerable<Pilot> pilots)
	{
		var random = new StableRandom(rngSeed);
		var order = pilots.ToList();
		random.Shuffle(order);
		return order.FirstOrDefault(pilot => random.NextDouble() < TriggerChance(pilot));
	}

	public static double TriggerChance(Pilot pilot) =>
		pilot.EffectivePersonality.ShiftedChance(
			WorldEventCatalog.Asteroid.BaseTriggerChance,
			WorldEventCatalog.Asteroid.ImpulsivenessSwing,
			EPilotPersonalityAxis.Deliberation,
			positiveSide: false);
}
