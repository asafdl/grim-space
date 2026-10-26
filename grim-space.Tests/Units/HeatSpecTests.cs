using GrimSpace.Units.Heat;

namespace GrimSpace.Tests.Units;

public sealed class HeatSpecTests
{
	[Fact]
	public void StandardParticipatesWithDefaultBudgets()
	{
		var spec = HeatSpec.Standard;

		Assert.True(spec.Participates);
		Assert.Equal(100, spec.Capacity);
		Assert.Equal(100, spec.EffectiveCapacity);
		Assert.Equal(20, spec.ClearPerTurn);
		Assert.Equal(1f, spec.GainMultiplier);
	}

	[Fact]
	public void NoneDoesNotParticipate()
	{
		var spec = HeatSpec.None;

		Assert.False(spec.Participates);
		Assert.Null(spec.Capacity);
		Assert.Equal(0, spec.EffectiveCapacity);
		Assert.Equal(0, spec.ScaleGain(20));
	}

	[Theory]
	[InlineData(10, 1f, 10)]
	[InlineData(10, 2f, 20)]
	[InlineData(5, 1.5f, 8)]
	[InlineData(0, 2f, 0)]
	public void ScaleGainAppliesMultiplier(int baseHeat, float multiplier, int expected)
	{
		var spec = new HeatSpec(100, 20, multiplier);

		Assert.Equal(expected, spec.ScaleGain(baseHeat));
	}
}
