using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Movement;

public sealed class MinPathApCostTests
{
	private const string ActorId = "torpedo";

	[Fact]
	public void TorpedoPathRetainsMinimumApRule()
	{
		var minPath = Stats.ForType(EType.Torpedo).MinPathApCost;
		var path = TorpedoPathSession.Begin(
			ActorId,
			Coord.Zero,
			BodyFrame.WorldAligned(Coord.Zero),
			momentumLevel: 0,
			minPath);

		path.ApplyStep(
			new TorpedoMoveStepAction(ActorId, ESpatialOrientation.Forward),
			Coord.Forward,
			stepApCost: 1,
			directionBit: 1);

		Assert.Equal(0, path.MinPathApRemaining);
		Assert.True(path.CanEnd(minPath));
	}

	[Fact]
	public void NormalShipsHaveNoMinimumPathApRule() =>
		Assert.Equal(0, Stats.ForType(EType.Fighter).MinPathApCost);
}
