using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.Tests.World.StarSystem.Generation;

public sealed class SupplySystemGenerationTests
{
	[Fact]
	public void Generate_SameSeed_ProducesIdenticalLayout()
	{
		var first = StarSystemGenerator.Generate(10, EStarSystemClass.Supply);
		var second = StarSystemGenerator.Generate(10, EStarSystemClass.Supply);

		Assert.Equal(
			first.PointsOfInterest.Select(poi => (poi.Id, poi.PlacedCenter, poi.Radius)),
			second.PointsOfInterest.Select(poi => (poi.Id, poi.PlacedCenter, poi.Radius)));
		Assert.Equal(first.DocksById.Keys, second.DocksById.Keys);
		foreach (var dockId in first.DocksById.Keys)
			Assert.Equal(first.DocksById[dockId].Position, second.DocksById[dockId].Position);
	}

	[Fact]
	public void Generate_SameSeed_ProducesIdenticalUnitChores()
	{
		var first = StarSystemGenerator.Generate(10, EStarSystemClass.Supply);
		var second = StarSystemGenerator.Generate(10, EStarSystemClass.Supply);

		Assert.Equal(
			first.Blueprint.UnitSpawns.Select(spawn => (spawn.Type, spawn.StartPoiId, spawn.ChorePoiIds)),
			second.Blueprint.UnitSpawns.Select(spawn => (spawn.Type, spawn.StartPoiId, spawn.ChorePoiIds)));
	}

	[Fact]
	public void Generate_UnitChores_AreNotAllIdenticalWithinType()
	{
		var world = StarSystemGenerator.Generate(42, EStarSystemClass.Supply);
		var spawns = world.Blueprint.UnitSpawns;

		Assert.True(HasChoreVariation(spawns, EType.ComplianceVessel));
		Assert.True(HasChoreVariation(spawns, EType.MiningBarge));
	}

	private static bool HasChoreVariation(IEnumerable<UnitSpawnIntent> spawns, EType type)
	{
		var signatures = spawns
			.Where(spawn => spawn.Type == type)
			.Select(spawn => (spawn.StartPoiId, Chore: string.Join(',', spawn.ChorePoiIds)))
			.Distinct()
			.ToArray();
		return signatures.Length > 1;
	}

	[Fact]
	public void Generate_UnitSpawns_AreDistributedAcrossDockedAndWorkingPhases()
	{
		var world = StarSystemGenerator.Generate(42, EStarSystemClass.Supply);
		var signatures = world.UnitRegistry.All
			.Select(unit => unit.State.Phase)
			.Distinct()
			.ToArray();

		Assert.True(signatures.Length > 1);
		Assert.Contains(EPhase.Docked, signatures);
		Assert.Contains(EPhase.Working, signatures);
	}

	[Fact]
	public void Generate_DifferentSeeds_ProduceDifferentLayouts()
	{
		var first = StarSystemGenerator.Generate(10, EStarSystemClass.Supply);
		var second = StarSystemGenerator.Generate(20, EStarSystemClass.Supply);

		Assert.NotEqual(
			first.PointsOfInterest.Select(poi => poi.PlacedCenter),
			second.PointsOfInterest.Select(poi => poi.PlacedCenter));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(7)]
	[InlineData(42)]
	[InlineData(99)]
	[InlineData(123)]
	[InlineData(500)]
	public void Generate_ManySeeds_SatisfyInvariants(int seed)
	{
		var world = StarSystemGenerator.Generate(seed, EStarSystemClass.Supply);
		var plan = world.Blueprint.SupplyPlan;

		Assert.Equal("copper", plan.ResourceId);
		Assert.Equal(7, world.PointsOfInterest.Count);
		Assert.Equal(6, world.DocksById.Count);
		Assert.Equal(8, world.RoutesById.Count);
		Assert.Equal(26, world.UnitRegistry.Ids.Count());
		Assert.Equal(world.Width * world.Height, world.PathfindingTerrain.Width * world.PathfindingTerrain.Height);

		Assert.Single(world.PointsOfInterest, poi => poi.LogicalRole == EPoiLogicalRole.Extraction);
		Assert.Single(world.PointsOfInterest, poi => poi.LogicalRole == EPoiLogicalRole.Refinery);
		Assert.Single(world.PointsOfInterest, poi => poi.LogicalRole == EPoiLogicalRole.Storage);
		Assert.Single(world.PointsOfInterest, poi => poi.LogicalRole == EPoiLogicalRole.Exit);
		Assert.Single(world.PointsOfInterest, poi => poi.LogicalRole == EPoiLogicalRole.Administrative);
		Assert.Single(world.PointsOfInterest, poi => poi.LogicalRole == EPoiLogicalRole.Trade);

		foreach (var template in world.Blueprint.PoiTemplates)
			Assert.Contains(world.PointsOfInterest, poi => poi.Id == template.Id);
	}
}
