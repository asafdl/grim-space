using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
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
	public void TorpedoCapabilitiesAreDisabled()
	{
		var caps = Capabilities.For(EType.Torpedo);

		Assert.Empty(caps);
	}

	[Fact]
	public void TorpedoAbilitiesAreDisabled()
	{
		var abilities = Capabilities.AbilitiesFor(EType.Torpedo);

		Assert.Empty(abilities);
	}

	[Fact]
	public void TorpedoHudIsDisabled()
	{
		Assert.Empty(AbilityHudCatalog.ForUnit(EType.Torpedo));
	}
}
