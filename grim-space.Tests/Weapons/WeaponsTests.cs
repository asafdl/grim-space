using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Weapons;

public sealed class WeaponsTests
{
	private static readonly BodyFrame Frame = BodyFrame.WorldAligned(new Coord(5, 5, 5));

	[Fact]
	public void RailgunBurstIsStraightLineThenForePyramid()
	{
		var cells = WeaponBursts.RailgunBurstCells(Frame, _ => true);

		Assert.Equal(26, cells.Count);
		for (var fore = 1; fore <= CombatConfig.RailgunLineLength; fore++)
			Assert.Contains(Frame.ToWorld(fore, 0, 0), cells);

		var pyramidApex = Frame.ToWorld(CombatConfig.RailgunLineLength, 0, 0);
		Assert.Contains(pyramidApex, cells);
		Assert.Contains(Frame.ToWorld(CombatConfig.RailgunLineLength + CombatConfig.RailgunPyramidRange, 1, 1), cells);
		Assert.Contains(Frame.ToWorld(CombatConfig.RailgunLineLength + CombatConfig.RailgunPyramidRange, -1, 1), cells);
		Assert.DoesNotContain(Frame.Origin, cells);
		Assert.DoesNotContain(Frame.ToWorld(CombatConfig.RailgunLineLength + CombatConfig.RailgunPyramidRange + 1, 0, 0), cells);
	}

	[Theory]
	[InlineData(ESpatialOrientation.Port)]
	[InlineData(ESpatialOrientation.Starboard)]
	public void FlakBurstIsThreeDimensionalPyramidFromMountTip(ESpatialOrientation mountedOn)
	{
		var cells = WeaponBursts.FlakBurstCells(Frame, mountedOn, _ => true);
		var apexPort = mountedOn == ESpatialOrientation.Port ? 1 : -1;
		var outwardStep = mountedOn == ESpatialOrientation.Port ? 1 : -1;
		var apex = Frame.ToWorld(0, apexPort, 0);
		var basePort = apexPort + outwardStep * CombatConfig.FlakRange;

		Assert.Equal(19, cells.Count);
		Assert.Contains(apex, cells);
		Assert.Single(cells, cell => cell == apex);
		Assert.Contains(Frame.ToWorld(0, basePort, 0), cells);
		Assert.Contains(Frame.ToWorld(1, basePort, 1), cells);
		Assert.Contains(Frame.ToWorld(-1, basePort, 1), cells);
		Assert.DoesNotContain(Frame.Origin, cells);
		Assert.DoesNotContain(Frame.ToWorld(3, apexPort, 0), cells);
		Assert.DoesNotContain(Frame.ToWorld(0, basePort + outwardStep, 0), cells);
	}

	[Fact]
	public void FlakMountedOnForCellUsesLateralSide()
	{
		var starboard = Frame.ToWorld(1, -1, 1);
		var port = Frame.ToWorld(1, 1, -1);

		Assert.Equal(ESpatialOrientation.Starboard, WeaponBursts.FlakMountedOnForCell(Frame, starboard));
		Assert.Equal(ESpatialOrientation.Port, WeaponBursts.FlakMountedOnForCell(Frame, port));
	}
}
