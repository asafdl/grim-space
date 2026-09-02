using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Generation;

namespace GrimSpace.Tests.World.StarSystem.Areas;

public sealed class AreaRadiusPickerTests
{
	[Fact]
	public void Pick_ClampsToConfiguredRange()
	{
		var config = new AreaRadiusConfig(MinRadius: 2, MaxRadius: 8, FractionOfSpan: 0.30);

		Assert.Equal(2, AreaRadiusPicker.Pick(1, config));
		Assert.Equal(8, AreaRadiusPicker.Pick(100, config));
	}

	[Fact]
	public void Pick_ScalesWithSpan()
	{
		var config = new AreaRadiusConfig(MinRadius: 1, MaxRadius: 100, FractionOfSpan: 0.25);

		Assert.Equal(25, AreaRadiusPicker.Pick(100, config));
		Assert.Equal(50, AreaRadiusPicker.Pick(200, config));
	}
}
