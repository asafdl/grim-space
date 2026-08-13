using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Units;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Units;

public sealed class TorpedoDomainTests
{
	[Fact]
	public void TorpedoStatsAreConfigured()
	{
		var stats = Stats.ForType(EType.Torpedo);

		Assert.Equal(1, stats.MaxHullPoints);
		Assert.Equal(0, stats.MaxShieldPoints.MaxOnAnyFace);
		Assert.Equal(1, stats.MinPathApCost);
		Assert.Equal(3, stats.MaxAp);
	}

	[Fact]
	public void TorpedoCapabilitiesAreMoveOnly()
	{
		var caps = Capabilities.For(EType.Torpedo);

		Assert.Equal([MoveDef.Instance], caps);
	}
}
