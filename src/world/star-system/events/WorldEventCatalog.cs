using GrimSpace.Math;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Ids;

namespace GrimSpace.World.StarSystem.Events;

public static class WorldEventCatalog
{
	internal static readonly AsteroidAccident Asteroid = new();

	private static readonly IReadOnlyList<Entry> Definitions = [Asteroid];

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
}
