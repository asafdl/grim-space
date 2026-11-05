using GrimSpace.Battle.Effects;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Run.Persistence;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class ReadOnlySetPersistenceTests
{
	[Fact]
	public void PersistenceRegistry_RoundTripsAreaAndGoopFactsWithCellSets()
	{
		var registry = PersistenceRegistry.CreateDefault();
		var cells = new HashSet<Coord> { new(1, 2, 3), new(4, 5, 6) };
		var area = new Record<AreaDamageFacts>(
			new AreaDamageFacts("source", EImpactCause.RamImpact, new Coord(1, 2, 3), cells));
		var goop = new Record<GoopSpawnedFacts>(
			new GoopSpawnedFacts("source", "hazard-1", new Coord(1, 2, 3), cells));

		var restoredArea = Assert.IsType<Record<AreaDamageFacts>>(
			registry.Read(registry.Write(area)));
		var restoredGoop = Assert.IsType<Record<GoopSpawnedFacts>>(
			registry.Read(registry.Write(goop)));

		Assert.Equal(cells, restoredArea.Value.Cells);
		Assert.Equal(cells, restoredGoop.Value.Cells);
	}
}
