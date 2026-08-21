using Godot;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Presentation;

namespace GrimSpace.Tests.World.StarSystem;

public sealed class MapMappingTests
{
	private const int Width = StarMap.DevMapWidth;
	private const int Height = StarMap.DevMapHeight;

	[Theory]
	[InlineData(0, 0)]
	[InlineData(512, 512)]
	[InlineData(1023, 1023)]
	public void RoundTrip_PreservesLogicalPoint(int x, int z)
	{
		var point = new Coord(x, 0, z);
		var world = MapMapping.ToWorld(point, Width, Height);
		var roundTrip = MapMapping.FromWorld(world, Width, Height);

		Assert.Equal(point, roundTrip);
	}

	[Fact]
	public void ToWorld_MaxPoint_MapsToPositiveQuadrantCorner()
	{
		var max = new Coord(1023, 0, 1023);
		var world = MapMapping.ToWorld(max, Width, Height);

		Assert.True(world.X > 0f);
		Assert.True(world.Z > 0f);
		Assert.Equal(0f, world.Y);
	}

	[Fact]
	public void ToWorld_Origin_MapsToNegativeQuadrantCorner()
	{
		var origin = new Coord(0, 0, 0);
		var world = MapMapping.ToWorld(origin, Width, Height);

		Assert.True(world.X < 0f);
		Assert.True(world.Z < 0f);
		Assert.Equal(0f, world.Y);
	}

	[Fact]
	public void MapExtent_MatchesLegacyVisualSize()
	{
		var extent = Width * MapMapping.WorldUnitsPerPoint;
		Assert.Equal(32f, extent, precision: 5);
	}
}
