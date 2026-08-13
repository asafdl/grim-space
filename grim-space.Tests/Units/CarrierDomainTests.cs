using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Units;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Units;

public sealed class CarrierDomainTests
{
	[Fact]
	public void CarrierStatsAreConfigured()
	{
		var stats = Stats.ForType(EType.Carrier);

		Assert.Equal(3, stats.MaxAp);
		Assert.Equal(2, stats.MaxHullPoints);
		Assert.Equal(2, stats.MaxShieldPoints.MaxOnAnyFace);
		Assert.Equal(0, stats.FlaksPerTurn);
		Assert.Equal(1, stats.RailgunsPerTurn);
	}

	[Fact]
	public void CarrierAbilitiesAreRailgunOnly()
	{
		var abilities = Capabilities.AbilitiesFor(EType.Carrier);

		Assert.Single(abilities, def => def is RailgunDef);
		Assert.DoesNotContain(abilities, def => def is FlakDef);
		Assert.DoesNotContain(abilities, def => def is SpawnPatrolDef);
	}
}
