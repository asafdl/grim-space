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
	public void WeightsFor_FiveCompletions_BlendsVeryLowAndLow()
	{
		var weights = ContractDangerProgression.WeightsFor(5);

		Assert.Equal(0.75f, weights[0], 3);
		Assert.Equal(0.25f, weights[1], 3);
		Assert.Equal(0f, weights[2]);
	}

	[Fact]
	public void WeightsFor_TwentyFiveCompletions_BlendsLowAndModerate()
	{
		var weights = ContractDangerProgression.WeightsFor(25);

		Assert.Equal(0f, weights[0]);
		Assert.Equal(0.75f, weights[1], 3);
		Assert.Equal(0.25f, weights[2], 3);
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
