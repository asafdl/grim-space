using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Units;

public sealed class TorpedoDomainTests
{
	[Fact]
	public void TorpedoStatsAreConfigured()
	{
		var stats = Stats.ForType(EType.Torpedo);

		Assert.Equal(1, stats.MaxHullPoints);
		Assert.Equal(0, stats.MaxShieldPointsPerFace);
		Assert.Equal(1, stats.MinPathApCost);
		Assert.Equal(3, stats.MaxAp);
	}

	[Fact]
	public void TorpedoCapabilitiesAreMoveOnly()
	{
		var caps = Capabilities.For(EType.Torpedo);

		Assert.Equal([MoveDef.Instance], caps);
	}

	[Fact]
	public void TurnOrderIsPlayerThenEnemyThenTorpedo()
	{
		var player = Factory.Create(
			new Instance { Id = "player", Type = EType.Fighter, Controller = EController.Player },
			Coord.Zero);
		var enemy = Factory.Create(
			new Instance { Id = "enemy", Type = EType.Patrol, Controller = EController.Enemy },
			new Coord(1, 0, 0));
		var torpedo = Factory.Create(
			new Instance { Id = "torpedo", Type = EType.Torpedo, Controller = EController.Player },
			new Coord(2, 0, 0));

		var order = TurnOrder.Living([torpedo, enemy, player]).Select(unit => unit.State.Id).ToList();

		Assert.Equal(["player", "enemy", "torpedo"], order);
	}
}
