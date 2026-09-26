using GrimSpace.Math;
using GrimSpace.World.StarSystem.Encounter;

namespace GrimSpace.World.StarSystem.Contracts.Generation;

public static class ContractDangerProgression
{
	/// <summary>Each completed contract shifts the danger center by 0.05 tiers (one full tier every 20 completions).</summary>
	public const double TierShiftPerCompletion = 0.05;

	/// <summary>Triangular falloff radius: tiers within this distance of the center keep non-zero weight.</summary>
	public const float TierSpread = 1.7f;

	public static float[] WeightsFor(int completedCount)
	{
		var weights = new float[5];
		if (completedCount <= 0)
		{
			weights[0] = 1f;
			return weights;
		}

		var center = (float)System.Math.Min(completedCount * TierShiftPerCompletion, 4);
		for (var i = 0; i < weights.Length; i++)
		{
			var distance = System.Math.Abs(i - center);
			weights[i] = System.Math.Max(0f, 1f - distance / TierSpread);
		}

		NormalizeInPlace(weights);
		return weights;
	}

	private static void NormalizeInPlace(float[] weights)
	{
		var total = 0f;
		foreach (var weight in weights)
			total += weight;

		if (total <= 0f)
		{
			weights[0] = 1f;
			return;
		}

		for (var i = 0; i < weights.Length; i++)
			weights[i] /= total;
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
