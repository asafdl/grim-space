using GrimSpace.Math;
using GrimSpace.World.StarSystem.Encounter;

namespace GrimSpace.World.StarSystem.Contracts.Generation;

public static class ContractDangerProgression
{
	/// <summary>Each completed contract advances the danger band by one quarter-step (full step every five completions).</summary>
	public const double TierShiftPerCompletion = 0.05;

	public static float[] WeightsFor(int completedCount)
	{
		var weights = new float[5];
		if (completedCount <= 0)
		{
			weights[0] = 1f;
			return weights;
		}

		var blended = completedCount * TierShiftPerCompletion;
		var lowerIndex = System.Math.Min((int)System.Math.Floor(blended), 4);
		if (lowerIndex >= 4)
		{
			weights[4] = 1f;
			return weights;
		}

		var frac = (float)(blended - lowerIndex);
		weights[lowerIndex] = 1f - frac;
		weights[lowerIndex + 1] = frac;
		return weights;
	}

	public static EDangerLevel RollDanger(StarMap map, int tick, int slot) =>
		RollDanger(map.Seed, tick, slot, map.ContractRegistry.CountCompleted());

	public static EDangerLevel RollDanger(int mapSeed, int tick, int slot, int completedCount)
	{
		var weights = WeightsFor(completedCount);
		var random = new StableRandom(
			StableSeedMixer.From(mapSeed).Add(tick).Add(slot).Add("contract-danger").Value);
		var index = PickWeightedIndex(weights, random);
		return (EDangerLevel)index;
	}

	private static int PickWeightedIndex(IReadOnlyList<float> weights, StableRandom random)
	{
		var total = 0f;
		foreach (var weight in weights)
			total += weight;

		if (total <= 0f)
			return 0;

		var roll = (float)(random.NextDouble() * total);
		var cumulative = 0f;
		for (var i = 0; i < weights.Count; i++)
		{
			cumulative += weights[i];
			if (roll < cumulative)
				return i;
		}

		return weights.Count - 1;
	}
}
