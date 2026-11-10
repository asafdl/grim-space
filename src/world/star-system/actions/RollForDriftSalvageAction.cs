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

public sealed record RollForDriftSalvageAction(
	string ActorId,
	ulong RngSeed) : IWorldEventRoll
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		RollForDriftSalvageDef.Instance;
}

public sealed class RollForDriftSalvageDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	private const ulong RewardStream = 0xD21F_7A11_5A17_A6E1UL;

	public static RollForDriftSalvageDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is RollForDriftSalvageAction roll
		&& string.Equals(roll.ActorId, StarSystemActorIds.WorldEvents, StringComparison.Ordinal);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var roll = (RollForDriftSalvageAction)action;
		var reward = RollReward(roll.RngSeed);
		if (reward.IsEmpty
			|| !WorldEventContexts.CanOpenPrompt(world)
			|| world.ItemInventory is null
			|| SelectFinder(roll.RngSeed, WorldEventContexts.RosterPilots(world)) is not { } finder
			|| !WorldEventContexts.IsTraveling(world, finder))
			return [];

		var pending = new PendingWorldEvent(
			$"world-event-{roll.RngSeed:x}",
			EWorldEventId.DriftSalvage,
			finder.Id,
			finder.AssignedShipId,
			0,
			WorldEventCatalog.Drift.Announce(finder.Name),
			reward);
		return
		[
			new ScheduleWorldEventActionEffect(
				1,
				new DriftSalvageFoundAction(StarSystemActorIds.WorldEvents, pending)),
		];
	}

	/// <summary>Checks pilots in shuffled order; the first to spot the derelict is the finder.</summary>
	public static Pilot? SelectFinder(ulong rngSeed, IEnumerable<Pilot> pilots)
	{
		var random = new StableRandom(rngSeed);
		var order = WorldEventContexts.RollOrder(random, pilots);
		return order.FirstOrDefault(pilot => random.NextDouble() < TriggerChance(pilot));
	}

	public static double TriggerChance(Pilot pilot) =>
		pilot.EffectivePersonality.ShiftedChance(
			WorldEventCatalog.Drift.BaseTriggerChance,
			WorldEventCatalog.Drift.DeliberationSwing,
			EPilotPersonalityAxis.Deliberation,
			positiveSide: true);

	public static ResourceBundle RollReward(ulong rngSeed)
	{
		var random = new StableRandom(rngSeed ^ RewardStream);
		if (random.NextDouble() < WorldEventCatalog.Drift.IndustrialCoreChance)
			return ResourceBundle.Of(ResourceId.IndustrialCore, 1);

		var scrap = random.TriangularWeightedNumber(
			WorldEventCatalog.Drift.ScrapMin,
			WorldEventCatalog.Drift.ScrapMax);
		return ResourceBundle.Of(ResourceId.ScrapAlloy, scrap);
	}
}
