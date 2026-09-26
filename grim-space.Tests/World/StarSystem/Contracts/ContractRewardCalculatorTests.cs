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
	public void Roll_HigherDanger_PaysMoreCreditsOnAverage()
	{
		const int samples = 40;
		var lowTotal = 0;
		var highTotal = 0;
		for (var index = 0; index < samples; index++)
		{
			var contractId = $"contract-{index}";
			lowTotal += Credits(ContractRewardCalculator.Roll(7, contractId, EContractKind.Hunt, EDangerLevel.Low));
			highTotal += Credits(ContractRewardCalculator.Roll(7, contractId, EContractKind.Hunt, EDangerLevel.VeryHigh));
		}

		Assert.True(highTotal > lowTotal);
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
