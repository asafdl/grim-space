using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Pathfinding;

namespace GrimSpace.Tests.World.StarSystem.Landmarks;

[StarSystemTestSuite]
public sealed class NavigationLandmarkPathfindingTests
{
	[Fact]
	public void LandmarkCellsInsideRadius_AreBlocked()
	{
		var map = StarMap.Create(12);
		var terrain = map.PathfindingTerrain;

		foreach (var landmark in map.NavigationLandmarks)
		{
			var radiusSquared = (long)landmark.Radius * landmark.Radius;
			for (var z = landmark.Position.Z - landmark.Radius; z <= landmark.Position.Z + landmark.Radius; z++)
			{
				for (var x = landmark.Position.X - landmark.Radius; x <= landmark.Position.X + landmark.Radius; x++)
				{
					var dx = x - landmark.Position.X;
					var dz = z - landmark.Position.Z;
					if (dx * (long)dx + dz * (long)dz > radiusSquared)
						continue;

					Assert.True(terrain[x, z].Blocked);
				}
			}
		}
	}

	[Fact]
	public void DockCells_RemainTraversable()
	{
		var map = StarMap.Create(44);
		foreach (var dock in map.DocksById.Values)
			Assert.True(map.PathfindingTerrain.IsTraversable(dock.Position));
	}
}
