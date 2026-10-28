using GrimSpace.World.StarSystem.Economy;

namespace GrimSpace.Tests.World.StarSystem.Economy;

public sealed class StrategicEconomyConfigTests
{
	[Theory]
	[InlineData(12, 1, 12)]
	[InlineData(12, 2, 17)]
	[InlineData(12, 3, 22)]
	public void SalaryFor_UsesBasePlusLevelAdditive(int baseSalary, int level, int expected) =>
		Assert.Equal(expected, StrategicEconomyConfig.SalaryFor(baseSalary, level));

	[Theory]
	[InlineData(0, 0)]
	[InlineData(1, 35)]
	[InlineData(2, 60)]
	[InlineData(3, 85)]
	[InlineData(4, 100)]
	public void RefuseChancePercent_RisesFromHazardMisses(int missed, int expected) =>
		Assert.Equal(expected, StrategicEconomyConfig.RefuseChancePercent(missed));

	[Theory]
	[InlineData(0, 0)]
	[InlineData(1, 0)]
	[InlineData(2, 2)]
	[InlineData(3, 17)]
	[InlineData(4, 32)]
	public void DepartChancePercent_RisesFromSalaryMisses(int missed, int expected) =>
		Assert.Equal(expected, StrategicEconomyConfig.DepartChancePercent(missed));
}
