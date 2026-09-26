using GrimSpace.Math;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Contracts;

/// <summary>
/// Contract completion payment (credits and bonus industrial cores on high-danger jobs).
/// Ship salvage amounts use <see cref="Run.LootCatalog"/> via <see cref="WreckageSalvageRoller"/>.
/// </summary>
public static class ContractRewardCalculator
{
	private const int HuntBaseCredits = 100;
	private const int DeliveryBaseCredits = 65;
	private const int WreckageBaseCredits = 75;
	private const float MaxDangerCreditMultiplier = 2.2f;
	private const float CreditVariance = 0.1f;

	public static ContractTerms Roll(int mapSeed, string contractId, EContractKind kind, EDangerLevel danger)
	{
		ArgumentException.ThrowIfNullOrEmpty(contractId);

		var rng = Rng(mapSeed, contractId, "contract-reward");
		var scaledCredits = (int)System.Math.Round(BaseCredits(kind) * DangerCreditMultiplier(danger));
		var credits = ApplyVariance(rng, scaledCredits);

		if (danger < EDangerLevel.High)
			return new ContractTerms(ResourceBundle.Of(ResourceId.Credits, credits));

		var cores = RollIndustrialCores(rng, danger);
		return new ContractTerms(ResourceBundle.Create(
			(ResourceId.Credits, credits),
			(ResourceId.IndustrialCore, cores)));
	}

	private static int BaseCredits(EContractKind kind) =>
		kind switch
		{
			EContractKind.Hunt => HuntBaseCredits,
			EContractKind.Delivery => DeliveryBaseCredits,
			EContractKind.Wreckage => WreckageBaseCredits,
			_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
		};

	private static StableRandom Rng(int mapSeed, string contractId, string scope) =>
		new(StableSeedMixer.From(mapSeed).Add(contractId).Add(scope).Value);

	private static int ApplyVariance(StableRandom rng, int scaled)
	{
		var variance = 1f + ((rng.NextDouble() * 2f) - 1f) * CreditVariance;
		return System.Math.Max(1, (int)System.Math.Round(scaled * variance));
	}

	private static float DangerCreditMultiplier(EDangerLevel danger)
	{
		var steps = (int)danger;
		if (steps == 0)
			return 1f;

		return MathF.Pow(MaxDangerCreditMultiplier, steps / (float)(int)EDangerLevel.VeryHigh);
	}

	private static int RollIndustrialCores(StableRandom rng, EDangerLevel danger)
	{
		var cores = danger == EDangerLevel.VeryHigh ? 2 : 1;
		if (rng.NextDouble() < 0.3)
			cores += 1;
		return cores;
	}
}
