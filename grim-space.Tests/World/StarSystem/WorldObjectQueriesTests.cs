using GrimSpace.Math.Grid;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.Tests.World.StarSystem;

public sealed class WorldObjectQueriesTests(DevStarMapFixture maps)
{
	[Fact]
	public void ResolveFocusable_PointOfInterest_ReturnsPlacedCenter()
	{
		var map = maps.Template(42);
		var poi = map.PointsOfInterest[0];

		var result = WorldObjectQueries.ResolveFocusable(
			map,
			poi.Id,
			_ => throw new InvalidOperationException("Unit position should not be requested."));

		Assert.Equal(new WorldObjectResolution.Found(poi.PlacedCenter), result);
	}

	[Fact]
	public void ResolveFocusable_Dock_ReturnsDockPosition()
	{
		var map = maps.Template(42);
		var dock = map.DocksById.Values.First();

		var result = WorldObjectQueries.ResolveFocusable(
			map,
			dock.Id,
			_ => throw new InvalidOperationException("Unit position should not be requested."));

		Assert.Equal(new WorldObjectResolution.Found(dock.Position), result);
	}

	[Fact]
	public void ResolveFocusable_Unit_UsesSuppliedLivePosition()
	{
		var map = maps.Template(42);
		var unitId = map.UnitRegistry.Ids.First();
		var livePosition = new Coord(123, 0, 456);
		string? requestedId = null;

		var result = WorldObjectQueries.ResolveFocusable(
			map,
			unitId,
			id =>
			{
				requestedId = id;
				return livePosition;
			});

		Assert.Equal(unitId, requestedId);
		Assert.Equal(new WorldObjectResolution.Found(livePosition), result);
	}

	[Fact]
	public void ResolveFocusable_MissingId_ReturnsMissing()
	{
		var result = WorldObjectQueries.ResolveFocusable(
			maps.Template(42),
			"missing-object",
			_ => throw new InvalidOperationException("Unit position should not be requested."));

		Assert.IsType<WorldObjectResolution.Missing>(result);
	}

	[Fact]
	public void ResolveFocusable_CrossCategoryCollision_ReturnsAmbiguous()
	{
		var map = maps.Fresh(42);
		var poiId = map.PointsOfInterest[0].Id;
		map.UnitRegistry.Add(Factory.CreatePirateFleet(
			poiId,
			new Coord(1, 0, 1),
			EFaction.Pirates,
			new CombatProfile(EDangerLevel.VeryLow, 1)));

		var result = WorldObjectQueries.ResolveFocusable(
			map,
			poiId,
			_ => throw new InvalidOperationException("Ambiguous unit position should not be requested."));

		Assert.IsType<WorldObjectResolution.Ambiguous>(result);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(7)]
	[InlineData(42)]
	[InlineData(99)]
	[InlineData(123)]
	[InlineData(500)]
	public void GeneratedWorld_FocusableIdsAreGloballyUnique(int seed)
	{
		var map = maps.Template(seed);
		var ids = map.PointsOfInterest.Select(poi => poi.Id)
			.Concat(map.DocksById.Values.Select(dock => dock.Id))
			.Concat(map.UnitRegistry.Ids)
			.ToArray();

		Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
	}
}
