using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Movement;

public sealed class MinPathApCostTests
{
	private const string ActorId = "actor";
	private static readonly int ShipMinPath = Stats.ForType(EType.Fighter).MinPathApCost;

	[Fact]
	public void BeginSeedsRemainingFromStats()
	{
		var path = MovePathSession.Begin(
			ActorId,
			Coord.Zero,
			BodyFrame.WorldAligned(Coord.Zero),
			momentumLevel: 0,
			minPathApCost: 1);

		Assert.Equal(1, path.MinPathApRemaining);
	}

	[Fact]
	public void CanEndUsesStatsFloor()
	{
		var path = MovePathSession.Begin(
			ActorId,
			Coord.Zero,
			BodyFrame.WorldAligned(Coord.Zero),
			0,
			minPathApCost: 1);
		path.ApplyStep(
			new MoveStepAction(ActorId, ESpatialOrientation.Forward),
			new Coord(1, 0, 0),
			stepApCost: 1,
			directionBit: 1);

		Assert.True(path.CanEnd(1));
	}

	[Fact]
	public void ShipDefaultStillRequiresThreeApSpent()
	{
		var path = MovePathSession.Begin(
			ActorId,
			Coord.Zero,
			BodyFrame.WorldAligned(Coord.Zero),
			0,
			ShipMinPath);
		path.ApplyStep(
			new MoveStepAction(ActorId, ESpatialOrientation.Forward),
			new Coord(1, 0, 0),
			stepApCost: 1,
			directionBit: 1);
		path.ApplyStep(
			new MoveStepAction(ActorId, ESpatialOrientation.Forward),
			new Coord(2, 0, 0),
			stepApCost: 1,
			directionBit: 1);

		Assert.False(path.CanEnd(ShipMinPath));

		path.ApplyStep(
			new MoveStepAction(ActorId, ESpatialOrientation.Forward),
			new Coord(3, 0, 0),
			stepApCost: 1,
			directionBit: 1);

		Assert.True(path.CanEnd(ShipMinPath));
	}
}
