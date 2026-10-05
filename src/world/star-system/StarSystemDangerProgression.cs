using GrimSpace.Math;
using GrimSpace.World.StarSystem.Encounter;

namespace GrimSpace.World.StarSystem;

public static class StarSystemDangerProgression
{
	/// <summary>Initial danger progression is 0.1 tiers per completion, easing near the middle and upper tiers.</summary>
	public const double TierShiftPerCompletion = 0.1;

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

		var center = ProgressionCenterFor(completedCount);
		for (var i = 0; i < weights.Length; i++)
		{
			var distance = System.Math.Abs(i - center);
			weights[i] = System.Math.Max(0f, 1f - distance / TierSpread);
		}

		NormalizeInPlace(weights);
		return weights;
	}

	private static float ProgressionCenterFor(int completedCount)
	{
		const float maximumCenter = 4f;
		var normalizedProgress = (float)(completedCount * TierShiftPerCompletion / maximumCenter);
		return maximumCenter * (1f - System.MathF.Exp(-normalizedProgress));
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

	public static EDangerLevel RollDanger(StarMap map, int tick, int slot, string rngScope)
	{
		ArgumentException.ThrowIfNullOrEmpty(rngScope);
		return RollDanger(map.Seed, tick, slot, map.ContractRegistry.CountCompleted(), rngScope);
	}

	public static EDangerLevel RollDanger(
		int mapSeed,
		int tick,
		int slot,
		int completedContractCount,
		string rngScope)
	{
		ArgumentException.ThrowIfNullOrEmpty(rngScope);

		var weights = WeightsFor(completedContractCount);
		var random = new StableRandom(
			StableSeedMixer.From(mapSeed).Add(tick).Add(slot).Add(rngScope).Value);
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
