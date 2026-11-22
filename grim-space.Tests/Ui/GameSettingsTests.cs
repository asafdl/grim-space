using GrimSpace.Application;

namespace GrimSpace.Tests.Ui;

public sealed class GameSettingsTests
{
	[Theory]
	[InlineData(0f, 0.001f)]
	[InlineData(0.002f, 0.002f)]
	[InlineData(0.004f, 0.004f)]
	[InlineData(0.04f, 0.01f)]
	[InlineData(1f, 0.01f)]
	public void NormalizeBattleCameraSensitivity_PreservesRatesWithinDisplayBounds(float value, float expected)
	{
		Assert.Equal(expected, GameSettings.NormalizeBattleCameraSensitivity(value));
	}

	[Theory]
	[InlineData(float.NaN)]
	[InlineData(float.PositiveInfinity)]
	[InlineData(float.NegativeInfinity)]
	public void NormalizeBattleCameraSensitivity_RejectsNonFiniteValues(float value)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => GameSettings.NormalizeBattleCameraSensitivity(value));
	}
}
