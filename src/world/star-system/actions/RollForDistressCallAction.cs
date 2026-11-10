using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record RollForDistressCallAction(
	string ActorId,
	ulong RngSeed) : IWorldEventRoll
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		RollForDistressCallDef.Instance;
}

public sealed class RollForDistressCallDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	private const ulong RewardStream = 0xD157_4E55_C411_A11DUL;
	private const ulong RecruitStream = 0xD157_4E55_C411_B0D1UL;

	public static RollForDistressCallDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is RollForDistressCallAction roll
		&& string.Equals(roll.ActorId, StarSystemActorIds.WorldEvents, StringComparison.Ordinal);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var roll = (RollForDistressCallAction)action;
		var rosterHasRoom = world.PilotRegistry?.CanRegister == true;
		if (!WorldEventContexts.CanOpenPrompt(world)
			|| world.ItemInventory is null
			|| SelectResponder(roll.RngSeed, WorldEventContexts.RosterPilots(world), rosterHasRoom) is not { } responder
			|| !WorldEventContexts.IsTraveling(world, responder))
			return [];

		PendingWorldEvent pending;
		if (responder.EffectivePersonality.Allegiance > 0)
		{
			var recruit = RollRecruit(roll.RngSeed);
			pending = new PendingWorldEvent(
				$"world-event-{roll.RngSeed:x}",
				EWorldEventId.DistressCall,
				responder.Id,
				responder.AssignedShipId,
				0,
				WorldEventCatalog.Distress.AnnounceSurvivor(responder.Name, recruit.Name),
				Recruit: recruit);
		}
		else
		{
			pending = new PendingWorldEvent(
				$"world-event-{roll.RngSeed:x}",
				EWorldEventId.DistressCall,
				responder.Id,
				responder.AssignedShipId,
				0,
				WorldEventCatalog.Distress.AnnounceKill(responder.Name),
				RollReward(roll.RngSeed));
		}
		return
		[
			new ScheduleWorldEventActionEffect(
				1,
				new DistressCallReceivedAction(StarSystemActorIds.WorldEvents, pending)),
		];
	}

	/// <summary>
	/// Checks eligible pilots in shuffled order, skipping recruits when the roster is full.
	/// The first to answer decides the outcome by their allegiance.
	/// </summary>
	public static Pilot? SelectResponder(ulong rngSeed, IEnumerable<Pilot> pilots, bool rosterHasRoom = true)
	{
		var random = new StableRandom(rngSeed);
		var order = WorldEventContexts.RollOrder(random, pilots);
		foreach (var pilot in order)
		{
			var loyal = pilot.EffectivePersonality.Allegiance > 0;
			if (loyal && !rosterHasRoom)
				continue;
			if (random.NextDouble() < TriggerChance(pilot, loyal))
				return pilot;
		}

		return null;
	}

	public static double TriggerChance(Pilot pilot, bool positiveSide) =>
		pilot.EffectivePersonality.ShiftedChance(
			WorldEventCatalog.Distress.BaseTriggerChance,
			WorldEventCatalog.Distress.AllegianceSwing,
			EPilotPersonalityAxis.Allegiance,
			positiveSide);

	public static ResourceBundle RollReward(ulong rngSeed)
	{
		var random = new StableRandom(rngSeed ^ RewardStream);
		var scrap = random.TriangularWeightedNumber(
			WorldEventCatalog.Distress.ScrapMin,
			WorldEventCatalog.Distress.ScrapMax);
		var credits = random.TriangularWeightedNumber(
			WorldEventCatalog.Distress.CreditsMin,
			WorldEventCatalog.Distress.CreditsMax);
		return ResourceBundle.Create(
			(ResourceId.ScrapAlloy, scrap),
			(ResourceId.Credits, credits));
	}

	public static PilotRecruitProfile RollRecruit(ulong rngSeed)
	{
		var random = new StableRoll(rngSeed ^ RecruitStream);
		var name = Pilot.RollRecruitDisplayName(random);
		var portraitId = Pilot.RollRecruitPortraitId(random);
		var backgrounds = PilotRecruitment.RecruitableBackgroundIds;
		var classes = PilotRecruitment.RecruitableOperatorClassIds;
		var backgroundId = backgrounds[random.Next(backgrounds.Count)];
		var classId = classes[random.Next(classes.Count)];
		var baseSalary = StrategicEconomyConfig.RollBaseSalaryCredits(random);
		var quirks = PilotQuirkCatalog.RollQuirks(random);
		return new PilotRecruitProfile(
			name,
			portraitId,
			backgroundId,
			positiveQuirkIds: quirks.Where(id =>
				PilotQuirkCatalog.Get(id).Polarity == EPilotQuirkPolarity.Positive).ToArray(),
			negativeQuirkIds: quirks.Where(id =>
				PilotQuirkCatalog.Get(id).Polarity == EPilotQuirkPolarity.Negative).ToArray(),
			experiencePoints: 0,
			claimedLevel: PilotProgression.LevelFor(0),
			baseSalaryCredits: baseSalary,
			classId: classId);
	}

	/// <summary>
	/// Feeds the recruitment helpers from <see cref="StableRandom"/>.
	/// Those helpers take <see cref="System.Random"/>; this only forwards its stream.
	/// </summary>
	private sealed class StableRoll : Random
	{
		private StableRandom _random;

		public StableRoll(ulong seed)
			: base(0) =>
			_random = new StableRandom(seed);

		public override int Next() => Next(int.MaxValue);

		public override int Next(int maxValue)
		{
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxValue);
			return (int)(NextDouble() * maxValue);
		}

		public override int Next(int minValue, int maxValue)
		{
			ArgumentOutOfRangeException.ThrowIfGreaterThan(minValue, maxValue);
			var range = (long)maxValue - minValue;
			return range <= 1 ? minValue : minValue + Next((int)range);
		}

		public override double NextDouble() => _random.NextDouble();

		protected override double Sample() => NextDouble();
	}
}
