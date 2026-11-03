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
		var maneuverability = ShipCatalog.SpecFor(EType.RepurposedMiner).Maneuverability;
		var configuration = ShipCatalog.NewRunLoadoutFor(EType.RepurposedMiner);

		var maxShields = configuration.BaseShieldPoints;

		Assert.Equal(4, maneuverability.BaseActionPoints);
		Assert.Equal((1 * 100), configuration.BaseHullPoints);
		Assert.Equal((3 * 100), maxShields[GrimSpace.Math.Grid.ESpatialOrientation.Forward]);
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
