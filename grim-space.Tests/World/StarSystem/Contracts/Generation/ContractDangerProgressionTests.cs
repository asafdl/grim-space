using GrimSpace.World.StarSystem.Contracts.Generation;
using GrimSpace.World.StarSystem.Encounter;

namespace GrimSpace.Tests.World.StarSystem.Contracts.Generation;

[StarSystemTestSuite]
public sealed class ContractDangerProgressionTests
{
	[Fact]
	public void WeightsFor_ZeroCompletions_IsVeryLowOnly()
	{
		var weights = ContractDangerProgression.WeightsFor(0);

		Assert.Equal(1f, weights[0]);
		Assert.Equal(0f, weights[1]);
		Assert.Equal(0f, weights[2]);
	}

	[Fact]
	public void WeightsFor_FiveCompletions_SpreadsAcrossVeryLowAndLow()
	{
		var weights = ContractDangerProgression.WeightsFor(5);

		Assert.True(weights[0] > weights[1]);
		Assert.Equal(0f, weights[2]);
	}

	[Fact]
	public void WeightsFor_TwentyFiveCompletions_SpreadsAcrossThreeTiers()
	{
		var weights = ContractDangerProgression.WeightsFor(25);

		Assert.True(weights[0] > 0f);
		Assert.True(weights[1] > weights[0]);
		Assert.True(weights[2] > 0f);
		Assert.True(weights[1] > weights[2]);
		Assert.Equal(0f, weights[3]);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(5)]
	[InlineData(40)]
	public void WeightsFor_SumToOne(int completed)
	{
		var weights = ContractDangerProgression.WeightsFor(completed);
		Assert.Equal(1f, weights.Sum(), 3);
	}

	[Fact]
	public void RollDanger_SameSeed_IsDeterministic()
	{
		var first = ContractDangerProgression.RollDanger(42, 10, 0, 5);
		var second = ContractDangerProgression.RollDanger(42, 10, 0, 5);

		Assert.Equal(first, second);
	}

	[Fact]
	public void RollDanger_ZeroCompletions_IsAlwaysVeryLow()
	{
		for (var slot = 0; slot < 20; slot++)
			Assert.Equal(EDangerLevel.VeryLow, ContractDangerProgression.RollDanger(99, 1, slot, 0));
	}
}
