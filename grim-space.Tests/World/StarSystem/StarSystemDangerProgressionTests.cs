using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem;

[StarSystemTestSuite]
public sealed class StarSystemDangerProgressionTests
{
	[Fact]
	public void WeightsFor_ZeroCompletions_IsVeryLowOnly()
	{
		var weights = StarSystemDangerProgression.WeightsFor(0);

		Assert.Equal(1f, weights[0]);
		Assert.Equal(0f, weights[1]);
		Assert.Equal(0f, weights[2]);
	}

	[Fact]
	public void WeightsFor_FiveCompletions_SpreadsAcrossVeryLowAndLow()
	{
		var weights = StarSystemDangerProgression.WeightsFor(5);

		Assert.True(weights[0] > weights[1]);
		Assert.True(weights[1] > weights[2]);
		Assert.Equal(0f, weights[3]);
	}

	[Fact]
	public void WeightsFor_TenCompletions_ReachesModerateTier()
	{
		var weights = StarSystemDangerProgression.WeightsFor(10);

		Assert.True(weights[0] > 0f);
		Assert.True(weights[1] > weights[0]);
		Assert.True(weights[2] > 0f);
		Assert.Equal(0f, weights[3]);
	}

	[Fact]
	public void WeightsFor_TwentyFiveCompletions_SpendsMostWeightInMiddleTiers()
	{
		var weights = StarSystemDangerProgression.WeightsFor(25);

		Assert.Equal(0f, weights[0]);
		Assert.True(weights[1] > 0f);
		Assert.True(weights[2] > 0f);
		Assert.True(weights[3] > 0f);
		Assert.Equal(0f, weights[4]);
		Assert.True(weights[2] > weights[1]);
		Assert.True(weights[2] > weights[3]);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(5)]
	[InlineData(40)]
	public void WeightsFor_SumToOne(int completed)
	{
		var weights = StarSystemDangerProgression.WeightsFor(completed);
		Assert.Equal(1f, weights.Sum(), 3);
	}

	[Fact]
	public void RollDanger_SameSeed_IsDeterministic()
	{
		var first = StarSystemDangerProgression.RollDanger(42, 10, 0, 5, "contract-danger");
		var second = StarSystemDangerProgression.RollDanger(42, 10, 0, 5, "contract-danger");

		Assert.Equal(first, second);
	}

	[Fact]
	public void RollDanger_ZeroCompletions_IsAlwaysVeryLow()
	{
		for (var slot = 0; slot < 20; slot++)
			Assert.Equal(EDangerLevel.VeryLow, StarSystemDangerProgression.RollDanger(99, 1, slot, 0, "contract-danger"));
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(0)]
	public void RollDanger_NonPositiveCompletions_IsAlwaysVeryLow(int completedCount)
	{
		for (var slot = 0; slot < 20; slot++)
			Assert.Equal(
				EDangerLevel.VeryLow,
				StarSystemDangerProgression.RollDanger(99, 1, slot, completedCount, "contract-danger"));
	}

	[Fact]
	public void RollDanger_DifferentScopes_ProduceIndependentDeterministicStreams()
	{
		var contractFirst = StarSystemDangerProgression.RollDanger(42, 10, 0, 5, "contract-danger");
		var contractSecond = StarSystemDangerProgression.RollDanger(42, 10, 0, 5, "contract-danger");
		var otherScopeFirst = StarSystemDangerProgression.RollDanger(42, 10, 0, 5, "other-danger");
		var otherScopeSecond = StarSystemDangerProgression.RollDanger(42, 10, 0, 5, "other-danger");

		Assert.Equal(contractFirst, contractSecond);
		Assert.Equal(otherScopeFirst, otherScopeSecond);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public void RollDanger_RejectsMissingScope(string? rngScope)
	{
		Assert.ThrowsAny<ArgumentException>(() =>
			StarSystemDangerProgression.RollDanger(42, 10, 0, 5, rngScope!));
	}
}
