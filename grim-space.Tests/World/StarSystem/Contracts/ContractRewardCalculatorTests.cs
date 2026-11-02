using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class ContractRewardCalculatorTests
{
	[Fact]
	public void Roll_SameInputs_IsDeterministic()
	{
		var first = ContractRewardCalculator.Roll(42, "contract-a", EContractKind.Hunt, EDangerLevel.Low);
		var second = ContractRewardCalculator.Roll(42, "contract-a", EContractKind.Hunt, EDangerLevel.Low);

		Assert.Equal(first, second);
	}

	[Fact]
	public void Roll_EachDangerTier_PaysClearlyMoreThanThePrevious()
	{
		const int samples = 40;
		var dangers = new[]
		{
			EDangerLevel.VeryLow,
			EDangerLevel.Low,
			EDangerLevel.Moderate,
			EDangerLevel.High,
			EDangerLevel.VeryHigh,
		};

		foreach (var kind in new[] { EContractKind.Hunt, EContractKind.Delivery, EContractKind.Wreckage })
		{
			var previous = 0;
			for (var tier = 0; tier < dangers.Length; tier++)
			{
				var total = 0;
				for (var index = 0; index < samples; index++)
					total += Credits(ContractRewardCalculator.Roll(
						7,
						$"{kind}-{tier}-{index}",
						kind,
						dangers[tier]));

				if (tier > 0)
					Assert.True(total > previous * 13 / 10, $"{kind} tier {tier} did not clear 1.3x the previous tier.");
				previous = total;
			}
		}
	}

	[Fact]
	public void Roll_HighDanger_IncludesIndustrialCores()
	{
		var terms = ContractRewardCalculator.Roll(1, "hard-contract", EContractKind.Delivery, EDangerLevel.High);

		Assert.True(terms.Payment.TryGet(ResourceId.IndustrialCore, out var cores));
		Assert.InRange(cores, 1, 3);
	}

	[Fact]
	public void Roll_BelowHighDanger_DoesNotIncludeIndustrialCores()
	{
		var terms = ContractRewardCalculator.Roll(1, "moderate-contract", EContractKind.Delivery, EDangerLevel.Moderate);

		Assert.False(terms.Payment.TryGet(ResourceId.IndustrialCore, out _));
	}

	private static int Credits(ContractTerms terms)
	{
		Assert.True(terms.Payment.TryGet(ResourceId.Credits, out var credits));
		return credits;
	}
}
