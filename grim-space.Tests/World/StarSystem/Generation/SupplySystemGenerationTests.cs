using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.Tests.World.StarSystem.Generation;

public sealed class SupplySystemGenerationTests
{
	[Fact]
	public void Generate_SameSeed_ProducesIdenticalLayout()
	{
		var first = StarSystemGenerator.Generate(10, EStarSystemClass.Supply);
		var second = StarSystemGenerator.Generate(10, EStarSystemClass.Supply);

		Assert.Equal(
			first.PointsOfInterest.Select(poi => (poi.Id, poi.Center, poi.Radius)),
			second.PointsOfInterest.Select(poi => (poi.Id, poi.Center, poi.Radius)));
		Assert.Equal(first.DocksById.Keys, second.DocksById.Keys);
		foreach (var dockId in first.DocksById.Keys)
			Assert.Equal(first.DocksById[dockId].Position, second.DocksById[dockId].Position);
	}

	[Fact]
	public void Generate_DifferentSeeds_ProduceDifferentLayouts()
	{
		var first = StarSystemGenerator.Generate(10, EStarSystemClass.Supply);
		var second = StarSystemGenerator.Generate(20, EStarSystemClass.Supply);

		Assert.NotEqual(
			first.PointsOfInterest.Select(poi => poi.Center),
			second.PointsOfInterest.Select(poi => poi.Center));
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
		Assert.Equal(5, world.PointsOfInterest.Count);
		Assert.Equal(4, world.DocksById.Count);
		Assert.Equal(3, world.RoutesById.Count);
		Assert.Equal(4, world.UnitRegistry.Ids.Count());

		Assert.Single(world.PointsOfInterest, poi => poi.LogicalRole == EPoiLogicalRole.Extraction);
		Assert.Single(world.PointsOfInterest, poi => poi.LogicalRole == EPoiLogicalRole.Refinery);
		Assert.Single(world.PointsOfInterest, poi => poi.LogicalRole == EPoiLogicalRole.Storage);
		Assert.Single(world.PointsOfInterest, poi => poi.LogicalRole == EPoiLogicalRole.Exit);

		foreach (var spec in world.Blueprint.Pois)
			Assert.Contains(world.PointsOfInterest, poi => poi.Id == spec.Id);
	}
}
