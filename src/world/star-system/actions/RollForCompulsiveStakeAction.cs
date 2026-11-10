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

public sealed record RollForCompulsiveStakeAction(
	string ActorId,
	ulong RngSeed) : IWorldEventRoll
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		RollForCompulsiveStakeDef.Instance;
}

public sealed class RollForCompulsiveStakeDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	private const ulong StakeStream = 0xC0A7_51A5_E001_0001UL;
	private const ulong WalkStream = 0xC0A7_51A5_E001_0003UL;

	public static RollForCompulsiveStakeDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is RollForCompulsiveStakeAction roll
		&& string.Equals(roll.ActorId, StarSystemActorIds.WorldEvents, StringComparison.Ordinal);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var roll = (RollForCompulsiveStakeAction)action;
		if (!WorldEventContexts.CanOpenPrompt(world)
			|| world.ItemInventory is null
			|| SelectDemander(roll.RngSeed, WorldEventContexts.RosterPilots(world)) is not { } demander
			|| !WorldEventContexts.IsTraveling(world, demander))
			return [];

		var stake = RollStake(roll.RngSeed);
		var pending = new PendingWorldEvent(
			$"world-event-{roll.RngSeed:x}",
			EWorldEventId.CompulsiveStake,
			demander.Id,
			demander.AssignedShipId,
			0,
			WorldEventCatalog.Stake.Announce(demander.Name, stake),
			ResourceBundle.Of(ResourceId.Credits, -stake));
		return
		[
			new ScheduleWorldEventActionEffect(
				1,
				new CompulsiveStakeDemandedAction(StarSystemActorIds.WorldEvents, pending)),
		];
	}

	/// <summary>Checks pilots in shuffled order; the first compulsive one to crack demands the stake.</summary>
	public static Pilot? SelectDemander(ulong rngSeed, IEnumerable<Pilot> pilots)
	{
		var random = new StableRandom(rngSeed);
		var order = WorldEventContexts.RollOrder(random, pilots);
		return order.FirstOrDefault(pilot => random.NextDouble() < TriggerChance(pilot));
	}

	public static double TriggerChance(Pilot pilot) =>
		pilot.EffectivePersonality.ShiftedChance(
			WorldEventCatalog.Stake.BaseTriggerChance,
			WorldEventCatalog.Stake.CompulsionSwing,
			EPilotPersonalityAxis.Deliberation,
			positiveSide: false);

	public static int RollStake(ulong rngSeed)
	{
		var random = new StableRandom(rngSeed ^ StakeStream);
		return random.TriangularWeightedNumber(
			WorldEventCatalog.Stake.CreditsMin,
			WorldEventCatalog.Stake.CreditsMax);
	}

	public static bool WalksOnIgnore(string pendingId)
	{
		ArgumentException.ThrowIfNullOrEmpty(pendingId);
		var seed = StableSeedMixer.From(0).Add(pendingId).Add("compulsive-walk").Value ^ WalkStream;
		return new StableRandom(seed).NextDouble()
			< WorldEventCatalog.Stake.IgnoreWalkChancePercent / 100d;
	}
}
