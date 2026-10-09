using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Weapons;

[BattleTestSuite]
public sealed class WeaponsTests
{
	private const string PlayerId = "player";

	[Fact]
	public void LightningCannonBurstIsStraightLineThenForePyramid()
	{
		var (world, frame) = CreateWorld();
		var lightningCannon = CatalogExpectations.DefaultLightningCannonSpec();
		var cells = LightningCannonDef.Instance.AffectedCells(new LightningCannonAction(PlayerId), world);

		Assert.Equal(23, cells.Count);
		for (var fore = 1; fore <= lightningCannon.LineLength; fore++)
			Assert.Contains(frame.ToWorld(fore, 0, 0), cells);

		var pyramidApex = frame.ToWorld(lightningCannon.LineLength, 0, 0);
		Assert.Contains(pyramidApex, cells);
		Assert.Contains(frame.ToWorld(lightningCannon.LineLength + lightningCannon.PyramidRange, 1, 1), cells);
		Assert.Contains(frame.ToWorld(lightningCannon.LineLength + lightningCannon.PyramidRange, -1, 1), cells);
		Assert.DoesNotContain(frame.Origin, cells);
		Assert.DoesNotContain(frame.ToWorld(lightningCannon.LineLength + lightningCannon.PyramidRange + 1, 0, 0), cells);
	}

	[Theory]
	[InlineData(ESpatialOrientation.Port)]
	[InlineData(ESpatialOrientation.Starboard)]
	public void LightningCannonGetArea_SideMountsIncludeTerminalSpread(ESpatialOrientation mountedOn)
	{
		var origin = new Coord(10, 10, 10);
		var frame = BodyFrame.WorldAligned(origin);
		var spec = CatalogExpectations.DefaultLightningCannonSpec(EType.Gunship);
		var burst = frame.Step(mountedOn);
		var cells = spec.GetArea(origin, burst, frame.Fore, frame.Dorsal);

		Assert.True(cells.Count > spec.LineLength + spec.PyramidRange);
		Assert.Contains(origin + burst * (spec.LineLength + spec.PyramidRange) + frame.Fore + frame.Dorsal, cells);
	}

	[Theory]
	[InlineData(ESpatialOrientation.Port)]
	[InlineData(ESpatialOrientation.Starboard)]
	public void ScrapDroneSwarmBurstIsThreeDimensionalPyramidFromMountTip(ESpatialOrientation mountedOn)
	{
		var (world, frame) = CreateWorld();
		var swarm = CatalogExpectations.DefaultScrapDroneSwarmSpec();
		var cells = ScrapDroneSwarmDef.Instance.AffectedCells(new ScrapDroneSwarmAction(PlayerId, mountedOn), world);
		var apexPort = mountedOn == ESpatialOrientation.Port ? 1 : -1;
		var outwardStep = mountedOn == ESpatialOrientation.Port ? 1 : -1;
		var apex = frame.ToWorld(0, apexPort, 0);
		var basePort = apexPort + outwardStep * swarm.BurstRange;

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

	private static (BattleWorld World, BodyFrame Frame) CreateWorld(EType playerChassis = EType.Fighter)
	{
		var origin = new Coord(10, 10, 10);
		var player = playerChassis == EType.Fighter
			? BattleTestFixture.Player(origin)
			: Factory.Create(
				ShipInstance.FromCatalog(PlayerId, playerChassis),
				ETeam.Player,
				origin,
				new UserExecutionAgent());
		var battle = BattleTestFixture.BeginSimulation(
			player,
			BattleTestFixture.Enemy(Coord.Zero),
			BattleTestFixture.Grid(size: 30));
		var world = battle.PlayerAgent.Sim.World;
		return (world, BodyFrame.From(world.StateOf(PlayerId)));
	}
}
