using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record RollForDebrisDetourAction(
	string ActorId,
	ulong RngSeed) : IWorldEventRoll
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		RollForDebrisDetourDef.Instance;
}

public sealed class RollForDebrisDetourDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static RollForDebrisDetourDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is RollForDebrisDetourAction roll
		&& string.Equals(roll.ActorId, StarSystemActorIds.WorldEvents, StringComparison.Ordinal);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var roll = (RollForDebrisDetourAction)action;
		var cost = ResourceBundle.Of(ResourceId.Fuel, -WorldEventCatalog.DebrisDetour.FuelCost);
		if (!WorldEventContexts.CanOpenPrompt(world)
			|| world.ItemInventory is null
			|| !world.PlayerResources.CanApply(cost)
			|| SelectNavigator(roll.RngSeed, WorldEventContexts.RosterPilots(world)) is not { } navigator
			|| !WorldEventContexts.IsTraveling(world, navigator))
			return [];

		var pending = new PendingWorldEvent(
			$"world-event-{roll.RngSeed:x}",
			EWorldEventId.DebrisDetour,
			navigator.Id,
			navigator.AssignedShipId,
			0,
			WorldEventCatalog.DebrisDetour.Announce(navigator.Name),
			cost);
		return
		[
			new ScheduleWorldEventActionEffect(
				1,
				new DebrisDetourReportedAction(StarSystemActorIds.WorldEvents, pending)),
		];
	}

	/// <summary>Checks pilots in shuffled order; the first fearful one to divert is the navigator.</summary>
	public static Pilot? SelectNavigator(ulong rngSeed, IEnumerable<Pilot> pilots)
	{
		var random = new StableRandom(rngSeed);
		var order = WorldEventContexts.RollOrder(random, pilots);
		return order.FirstOrDefault(pilot => random.NextDouble() < TriggerChance(pilot));
	}

	public static double TriggerChance(Pilot pilot) =>
		pilot.EffectivePersonality.ShiftedChance(
			WorldEventCatalog.DebrisDetour.BaseTriggerChance,
			WorldEventCatalog.DebrisDetour.CautionSwing,
			EPilotPersonalityAxis.ThreatResponse,
			positiveSide: false);
}
