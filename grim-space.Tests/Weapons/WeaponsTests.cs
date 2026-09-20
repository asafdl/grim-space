using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Weapons;

public sealed class WeaponsTests
{
	private const string PlayerId = "player";

	[Fact]
	public void RailgunBurstIsStraightLineThenForePyramid()
	{
		var (world, frame) = CreateWorld();
		var railgun = CatalogExpectations.DefaultRailgunSpec();
		var cells = RailgunDef.Instance.AffectedCells(new RailgunAction(PlayerId), world);

		Assert.Equal(26, cells.Count);
		for (var fore = 1; fore <= railgun.LineLength; fore++)
			Assert.Contains(frame.ToWorld(fore, 0, 0), cells);

		var pyramidApex = frame.ToWorld(railgun.LineLength, 0, 0);
		Assert.Contains(pyramidApex, cells);
		Assert.Contains(frame.ToWorld(railgun.LineLength + railgun.PyramidRange, 1, 1), cells);
		Assert.Contains(frame.ToWorld(railgun.LineLength + railgun.PyramidRange, -1, 1), cells);
		Assert.DoesNotContain(frame.Origin, cells);
		Assert.DoesNotContain(frame.ToWorld(railgun.LineLength + railgun.PyramidRange + 1, 0, 0), cells);
	}

	[Theory]
	[InlineData(ESpatialOrientation.Port)]
	[InlineData(ESpatialOrientation.Starboard)]
	public void FlakBurstIsThreeDimensionalPyramidFromMountTip(ESpatialOrientation mountedOn)
	{
		var (world, frame) = CreateWorld();
		var flak = CatalogExpectations.DefaultFlakSpec();
		var cells = FlakDef.Instance.AffectedCells(new FlakAction(PlayerId, mountedOn), world);
		var apexPort = mountedOn == ESpatialOrientation.Port ? 1 : -1;
		var outwardStep = mountedOn == ESpatialOrientation.Port ? 1 : -1;
		var apex = frame.ToWorld(0, apexPort, 0);
		var basePort = apexPort + outwardStep * flak.BurstRange;

		Assert.Equal(19, cells.Count);
		Assert.Contains(apex, cells);
		Assert.Single(cells, cell => cell == apex);
		Assert.Contains(frame.ToWorld(0, basePort, 0), cells);
		Assert.Contains(frame.ToWorld(1, basePort, 1), cells);
		Assert.Contains(frame.ToWorld(-1, basePort, 1), cells);
		Assert.DoesNotContain(frame.Origin, cells);
		Assert.DoesNotContain(frame.ToWorld(3, apexPort, 0), cells);
		Assert.DoesNotContain(frame.ToWorld(0, basePort + outwardStep, 0), cells);
	}

	private static (BattleWorld World, BodyFrame Frame) CreateWorld()
	{
		var origin = new Coord(10, 10, 10);
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(origin),
			BattleTestFixture.Enemy(Coord.Zero),
			BattleTestFixture.Grid(size: 30));
		var world = battle.PlayerAgent.Sim.World;
		return (world, BodyFrame.From(world.StateOf(PlayerId)));
	}
}
