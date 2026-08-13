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
	[InlineData(EFlakMount.Port)]
	[InlineData(EFlakMount.Starboard)]
	public void FlakBurstIsThreeDimensionalPyramidFromMountTip(EFlakMount mount)
	{
		var config = FlakMountConfig.For(mount);
		var cells = WeaponBursts.FlakBurstCells(Frame, config, _ => true);
		var apexPort = mount == EFlakMount.Port ? 1 : -1;
		var outwardStep = mount == EFlakMount.Port ? 1 : -1;
		var apex = Frame.ToWorld(0, apexPort, 0);
		var basePort = apexPort + outwardStep * config.Range;

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
	public void FlakMountForCellUsesLateralSide()
	{
		var starboard = Frame.ToWorld(1, -1, 1);
		var port = Frame.ToWorld(1, 1, -1);

		Assert.Equal(EFlakMount.Starboard, WeaponBursts.FlakMountForCell(Frame, starboard));
		Assert.Equal(EFlakMount.Port, WeaponBursts.FlakMountForCell(Frame, port));
	}
}
