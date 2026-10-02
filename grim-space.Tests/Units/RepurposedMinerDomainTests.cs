using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Units;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Units;

[BattleTestSuite]
public sealed class RepurposedMinerDomainTests
{
	[Fact]
	public void RepurposedMinerStatsAreConfigured()
	{
		var stats = Stats.ForType(EType.RepurposedMiner);
		var configuration = ShipCatalog.NewRunLoadoutFor(EType.RepurposedMiner);

		var maxShields = configuration.MaxShieldPoints;

		Assert.Equal(4, stats.MaxAp);
		Assert.Equal(1, configuration.MaxHullPoints);
		Assert.Equal(3, maxShields[GrimSpace.Math.Grid.ESpatialOrientation.Forward]);
		Assert.Equal(0, maxShields[GrimSpace.Math.Grid.ESpatialOrientation.Retro]);
		Assert.Equal(2, CatalogExpectations.UsesPerTurn(EType.RepurposedMiner, EAbilityKind.ScrapDroneSwarm));
		Assert.Equal(0, CatalogExpectations.UsesPerTurn(EType.RepurposedMiner, EAbilityKind.LightningCannon));
	}

	[Fact]
	public void RepurposedMinerAbilitiesAreScrapDroneSwarmOnly()
	{
		var abilities = Capabilities.AbilitiesFor(EType.RepurposedMiner);

		Assert.Single(abilities, def => def is ScrapDroneSwarmDef);
		Assert.DoesNotContain(abilities, def => def is LightningCannonDef);
	}
}
