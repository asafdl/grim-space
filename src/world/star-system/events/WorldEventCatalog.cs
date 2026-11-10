using GrimSpace.Math;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Ids;

namespace GrimSpace.World.StarSystem.Events;

public static class WorldEventCatalog
{
	internal static readonly AsteroidAccident Asteroid = new();
	internal static readonly DriftSalvage Drift = new();
	internal static readonly DistressCall Distress = new();
	internal static readonly CompulsiveStake Stake = new();
	internal static readonly MinerPotshot Potshot = new();
	internal static readonly LongDetour DebrisDetour = new();

	private static readonly IReadOnlyList<Entry> Definitions =
		[Asteroid, Drift, Distress, Stake, Potshot, DebrisDetour];

	public static EWorldEventId Pick(double unitRoll) =>
		WeightedPick.Of(Definitions, entry => entry.Weight, unitRoll).Id;

	public static IWorldEventRoll CreateRoll(EWorldEventId id, ulong rngSeed) =>
		Definitions.Single(entry => entry.Id == id).CreateRoll(rngSeed);

	internal abstract class Entry
	{
		public abstract EWorldEventId Id { get; }
		public abstract int Weight { get; }
		public abstract IWorldEventRoll CreateRoll(ulong rngSeed);
	}

	internal sealed class AsteroidAccident : Entry
	{
		public int Damage => 15;
		public double BaseTriggerChance => 0;
		public double ImpulsivenessSwing => 0.20;

		public override EWorldEventId Id => EWorldEventId.AsteroidAccident;
		public override int Weight => 1;

		public override IWorldEventRoll CreateRoll(ulong rngSeed) =>
			new RollForAsteroidDodgeAction(StarSystemActorIds.WorldEvents, rngSeed);

		public WorldEventPresentation Announce(string pilotName, int shieldsLost, int hullLost) =>
			new(
				"Bumped Into an Asteroid",
				$"{pilotName} was showboating, trying to prove they can dodge the asteroid by \"Only 3cm!\". Ship takes {Hit(shieldsLost, hullLost)}.");

		private static string Hit(int shieldsLost, int hullLost) =>
			(shieldsLost, hullLost) switch
			{
				(> 0, > 0) => $"{shieldsLost} shield damage and {hullLost} hull damage",
				(> 0, _) => $"{shieldsLost} shield damage",
				_ => $"{hullLost} hull damage",
			};
	}

	internal sealed class DriftSalvage : Entry
	{
		public int ScrapMin => 80;
		public int ScrapMax => 160;
		public double IndustrialCoreChance => 0.02;
		public double BaseTriggerChance => 0;
		public double DeliberationSwing => 0.20;

		public override EWorldEventId Id => EWorldEventId.DriftSalvage;
		public override int Weight => 1;

		public override IWorldEventRoll CreateRoll(ulong rngSeed) =>
			new RollForDriftSalvageAction(StarSystemActorIds.WorldEvents, rngSeed);

		public WorldEventPresentation Announce(string pilotName) =>
			new(
				"Strange Signature Detected",
				$"Only lazy bots ignore chance for loot. Luckily {pilotName} is not one of them.");
	}

	internal sealed class DistressCall : Entry
	{
		public int ScrapMin => 80;
		public int ScrapMax => 160;
		public int CreditsMin => 40;
		public int CreditsMax => 100;
		public double BaseTriggerChance => 0;
		public double AllegianceSwing => 0.20;

		public override EWorldEventId Id => EWorldEventId.DistressCall;
		public override int Weight => 1;

		public override IWorldEventRoll CreateRoll(ulong rngSeed) =>
			new RollForDistressCallAction(StarSystemActorIds.WorldEvents, rngSeed);

		public WorldEventPresentation AnnounceKill(string pilotName) =>
			new(
				"Only a Fool Broadcasts His Location",
				$"{pilotName} answered the distress call and came back alone. The hold is heavier.");

		public WorldEventPresentation AnnounceSurvivor(string pilotName, string survivorName) =>
			new(
				"Survivor Wants to Join",
				$"{pilotName} investigated and found {survivorName} at a distress beacon, their ship is fried, but they say they can still fight.");
	}

	internal sealed class CompulsiveStake : Entry
	{
		public int CreditsMin => 50;
		public int CreditsMax => 120;
		public int IgnoreWalkChancePercent => 50;
		public double BaseTriggerChance => 0;
		public double CompulsionSwing => 0.20;

		public override EWorldEventId Id => EWorldEventId.CompulsiveStake;
		public override int Weight => 1;

		public override IWorldEventRoll CreateRoll(ulong rngSeed) =>
			new RollForCompulsiveStakeAction(StarSystemActorIds.WorldEvents, rngSeed);

		public WorldEventPresentation Announce(string pilotName, int credits) =>
			new(
				"I'm Good For It, I Promise",
				$"{pilotName} stops you and asks for {credits} credits. Look man, we've been on the road for a while, and I could use a little fun. I'm good for it, I promise!");

		public string IgnoreTooltipBody =>
			$"{IgnoreWalkChancePercent}% chance they leave the fleet.";
	}

	internal sealed class MinerPotshot : Entry
	{
		public int Damage => 20;
		public double BaseTriggerChance => 0;
		public double AggressionSwing => 0.20;

		public override EWorldEventId Id => EWorldEventId.Potshot;
		public override int Weight => 1;

		public override IWorldEventRoll CreateRoll(ulong rngSeed) =>
			new RollForPotshotAction(StarSystemActorIds.WorldEvents, rngSeed);

		public WorldEventPresentation Announce(string pilotName, int shieldsLost, int hullLost) =>
			new(
				"Just a Miner Drone",
				$"{pilotName} decided to have a bit of fun and take a potshot at a passing miner drone. Completely missing the cruiser that was escorting it. Ship takes {Hit(shieldsLost, hullLost)}.");

		private static string Hit(int shieldsLost, int hullLost) =>
			(shieldsLost, hullLost) switch
			{
				(> 0, > 0) => $"{shieldsLost} shield damage and {hullLost} hull damage",
				(> 0, _) => $"{shieldsLost} shield damage",
				_ => $"{hullLost} hull damage",
			};
	}

	internal sealed class LongDetour : Entry
	{
		public int FuelCost => 20;
		public double BaseTriggerChance => 0;
		public double CautionSwing => 0.20;

		public override EWorldEventId Id => EWorldEventId.DebrisDetour;
		public override int Weight => 1;

		public override IWorldEventRoll CreateRoll(ulong rngSeed) =>
			new RollForDebrisDetourAction(StarSystemActorIds.WorldEvents, rngSeed);

		public WorldEventPresentation Announce(string pilotName) =>
			new(
				"The Long Way Around",
				$"{pilotName} decided to take the \"safer\" route around some space debris. You never know what might be in there lurking. The detour burns [icon: Fuel, text: {FuelCost}].");
	}
}
