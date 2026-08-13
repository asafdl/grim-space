using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Units;

public sealed class PatrolDomainTests
{
	[Fact]
	public void PatrolStatsAreConfigured()
	{
		var stats = Stats.ForType(EType.Patrol);

		Assert.Equal(4, stats.MaxAp);
		Assert.Equal(1, stats.MaxHullPoints);
		Assert.Equal(3, stats.MaxShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(0, stats.MaxShieldPoints[ESpatialOrientation.Retro]);
		Assert.Equal(1, stats.FlaksPerTurn);
		Assert.Equal(0, stats.RailgunsPerTurn);
	}

	[Fact]
	public void PatrolAbilitiesAreFlakOnly()
	{
		var abilities = Capabilities.AbilitiesFor(EType.Patrol);

		Assert.Equal(2, abilities.Count);
		Assert.All(abilities, def => Assert.IsType<FlakDef>(def));
		Assert.DoesNotContain(abilities, def => def is RailgunDef);
	}
}
