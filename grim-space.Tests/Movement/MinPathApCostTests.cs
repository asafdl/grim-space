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
	public void ShipDefaultHasNoMinPathApFloor()
	{
		Assert.Equal(0, ShipMinPath);

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

		Assert.True(path.CanEnd(ShipMinPath));
	}

	[Fact]
	public void TorpedoStillRequiresMinPathApCost()
	{
		var minPath = Stats.ForType(EType.Torpedo).MinPathApCost;
		Assert.Equal(1, minPath);

		var path = MovePathSession.Begin(
			ActorId,
			Coord.Zero,
			BodyFrame.WorldAligned(Coord.Zero),
			0,
			minPath);
		Assert.Equal(1, path.MinPathApRemaining);

		path.ApplyStep(
			new MoveStepAction(ActorId, ESpatialOrientation.Forward),
			new Coord(1, 0, 0),
			stepApCost: 1,
			directionBit: 1);

		Assert.Equal(0, path.MinPathApRemaining);
		Assert.True(path.CanEnd(minPath));
	}
}
