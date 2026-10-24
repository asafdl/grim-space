using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class MerchantAvailabilityTests
{
	[Theory]
	[InlineData(0, EMerchantAvailabilityBand.T0)]
	[InlineData(3, EMerchantAvailabilityBand.T0)]
	[InlineData(4, EMerchantAvailabilityBand.T0_5)]
	[InlineData(7, EMerchantAvailabilityBand.T0_5)]
	[InlineData(8, EMerchantAvailabilityBand.T1)]
	[InlineData(12, EMerchantAvailabilityBand.T1_5)]
	[InlineData(16, EMerchantAvailabilityBand.T2)]
	[InlineData(100, EMerchantAvailabilityBand.T2)]
	public void BandForCompletedCount_UsesInclusiveCompletionBars(
		int completed,
		EMerchantAvailabilityBand expected)
	{
		Assert.Equal(expected, MerchantAvailability.BandForCompletedCount(completed));
	}

	[Theory]
	[InlineData(0, EMerchantAvailabilityBand.T0)]
	[InlineData(1, EMerchantAvailabilityBand.T0_5)]
	[InlineData(2, EMerchantAvailabilityBand.T1)]
	[InlineData(3, EMerchantAvailabilityBand.T1_5)]
	[InlineData(4, EMerchantAvailabilityBand.T2)]
	public void CurrentBand_FollowsDebugProgressionBars(int debugBumpCount, EMerchantAvailabilityBand expected)
	{
		var map = StarMap.Create(42);
		for (var i = 0; i < debugBumpCount; i++)
			map.BumpDebugDangerProgression();

		Assert.Equal(expected, MerchantAvailability.CurrentBand(map));
	}

	[Fact]
	public void CurrentBand_DebugProgressionContributes()
	{
		var map = StarMap.Create(7);
		Assert.Equal(EMerchantAvailabilityBand.T0, MerchantAvailability.CurrentBand(map));

		map.BumpDebugDangerProgression();
		Assert.Equal(EMerchantAvailabilityBand.T0_5, MerchantAvailability.CurrentBand(map));
	}

	[Fact]
	public void CurrentBand_RepeatedQueries_AreDeterministicAndDoNotMutateMap()
	{
		var map = StarMap.Create(99);
		var completedBefore = map.ContractRegistry.CountCompleted();

		var first = MerchantAvailability.CurrentBand(map);
		var second = MerchantAvailability.CurrentBand(map);

		Assert.Equal(first, second);
		Assert.Equal(completedBefore, map.ContractRegistry.CountCompleted());
	}

}
