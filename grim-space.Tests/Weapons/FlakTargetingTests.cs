using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Weapons;

public sealed class FlakTargetingTests
{
	private static readonly BodyFrame Frame = BodyFrame.WorldAligned(new Coord(5, 5, 5));

	[Theory]
	[InlineData(EFlakMount.Port)]
	[InlineData(EFlakMount.Starboard)]
	public void BurstIsThreeDimensionalPyramidFromMountTip(EFlakMount mount)
	{
		var config = FlakMountConfig.For(mount);
		var cells = FlakTargeting.GetBurstCells(Frame, config, _ => true);
		var apexPort = mount == EFlakMount.Port ? 1 : -1;
		var outwardStep = mount == EFlakMount.Port ? 1 : -1;
		var apex = Frame.ToWorld(0, apexPort, 0);
		var basePort = apexPort + outwardStep * config.Range;

		Assert.Equal(44, cells.Count);
		Assert.Contains(apex, cells);
		Assert.Single(cells, cell => cell == apex);
		Assert.Contains(Frame.ToWorld(0, basePort, 0), cells);
		Assert.Contains(Frame.ToWorld(2, basePort, 1), cells);
		Assert.Contains(Frame.ToWorld(-2, basePort, 1), cells);
		Assert.DoesNotContain(Frame.Origin, cells);
		Assert.DoesNotContain(Frame.ToWorld(4, apexPort, 0), cells);
		Assert.DoesNotContain(Frame.ToWorld(0, basePort + outwardStep, 0), cells);
	}

	[Fact]
	public void MountForCellUsesLateralSide()
	{
		var starboard = Frame.ToWorld(1, -1, 1);
		var port = Frame.ToWorld(1, 1, -1);

		Assert.Equal(EFlakMount.Starboard, FlakTargeting.MountForCell(Frame, starboard));
		Assert.Equal(EFlakMount.Port, FlakTargeting.MountForCell(Frame, port));
	}
}
