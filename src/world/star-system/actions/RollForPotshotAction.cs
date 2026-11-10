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

public sealed record RollForPotshotAction(
	string ActorId,
	ulong RngSeed) : IWorldEventRoll
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		RollForPotshotDef.Instance;
}

public sealed class RollForPotshotDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static RollForPotshotDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is RollForPotshotAction roll
		&& string.Equals(roll.ActorId, StarSystemActorIds.WorldEvents, StringComparison.Ordinal);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var roll = (RollForPotshotAction)action;
		if (!WorldEventContexts.CanOpenPrompt(world)
			|| world.ItemInventory is null
			|| SelectShooter(roll.RngSeed, WorldEventContexts.FieldedPilots(world)) is not { } shooter
			|| !WorldEventContexts.IsTraveling(world, shooter)
			|| !world.ItemInventory.TryGet(shooter.AssignedShipId!, out var ship))
			return [];

		var hit = FieldedShipDamage.Apply(ship, WorldEventCatalog.Potshot.Damage, minimumHull: 1);
		var pending = new PendingWorldEvent(
			$"world-event-{roll.RngSeed:x}",
			EWorldEventId.Potshot,
			shooter.Id,
			shooter.AssignedShipId!,
			WorldEventCatalog.Potshot.Damage,
			WorldEventCatalog.Potshot.Announce(
				shooter.Name,
				ship.TotalCurrentShieldPoints - hit.TotalCurrentShieldPoints,
				ship.CurrentHullPoints - hit.CurrentHullPoints));
		return
		[
			new ScheduleWorldEventActionEffect(
				1,
				new PotshotReportedAction(StarSystemActorIds.WorldEvents, pending)),
		];
	}

	/// <summary>Checks pilots in shuffled order; the first aggressive one to fire is the shooter.</summary>
	public static Pilot? SelectShooter(ulong rngSeed, IEnumerable<Pilot> pilots)
	{
		var random = new StableRandom(rngSeed);
		var order = WorldEventContexts.RollOrder(random, pilots);
		return order.FirstOrDefault(pilot => random.NextDouble() < TriggerChance(pilot));
	}

	public static double TriggerChance(Pilot pilot) =>
		pilot.EffectivePersonality.ShiftedChance(
			WorldEventCatalog.Potshot.BaseTriggerChance,
			WorldEventCatalog.Potshot.AggressionSwing,
			EPilotPersonalityAxis.ThreatResponse,
			positiveSide: true);
}
