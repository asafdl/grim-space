using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Weapons;

public sealed class RailgunTargetingTests
{
	private static readonly BodyFrame Frame = BodyFrame.WorldAligned(new Coord(5, 5, 5));

	[Fact]
	public void BurstIsStraightLineThenForePyramid()
	{
		var cells = RailgunTargeting.GetBurstCells(Frame, _ => true);

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
}
