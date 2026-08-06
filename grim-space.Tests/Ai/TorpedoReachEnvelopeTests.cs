using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Actions;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Ai;

public sealed class TorpedoReachEnvelopeTests
{
	private const string PlayerId = "player";

	[Fact]
	public void Build_HasOneLayerPerFuel()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		FaceForward(battle, torpedoId);
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = 2;

		var envelope = TorpedoReachEnvelope.Build(battle.Engine.CreateSimulation(), torpedoId);

		Assert.Equal(2, envelope.Count);
		Assert.NotEmpty(envelope.Layers[0]);
		Assert.NotEmpty(envelope.Layers[1]);
	}

	[Fact]
	public void Build_UsesStatsMaxApNotHardcoded()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		FaceForward(battle, torpedoId);
		var torpedo = battle.Engine.World.StateOf(torpedoId);
		torpedo.FuelRemaining = 1;
		torpedo.MomentumLevel = 0;
		torpedo.ActionPoints = torpedo.Stats.MaxAp;

		var envelope = TorpedoReachEnvelope.Build(battle.Engine.CreateSimulation(), torpedoId);
		var start = torpedo.Position;

		Assert.Contains(start, envelope.Layers[0]);
		Assert.True(envelope.Layers[0].Count > 1);
	}

	[Fact]
	public void WithinBlast_TrueForEnemyOnThisTurnCorridor()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		FaceForward(battle, torpedoId);
		var start = battle.Engine.World.StateOf(torpedoId).Position;
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = 1;
		battle.Engine.World.StateOf(PlayerId).Position = new Coord(0, 0, 0);
		var enemyPos = start + Coord.Forward * 2;

		var envelope = TorpedoReachEnvelope.Build(battle.Engine.CreateSimulation(), torpedoId);

		Assert.True(envelope.WithinBlast(0, enemyPos));
	}

	[Fact]
	public void WithinBlast_FalseWhenEnemyFarBehindBeyondLegalReach()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		FaceForward(battle, torpedoId);
		var start = new Coord(5, 5, 10);
		battle.Engine.World.StateOf(torpedoId).Position = start;
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = 1;
		battle.Engine.World.StateOf(torpedoId).MomentumLevel = 0;
		battle.Engine.World.StateOf(PlayerId).Position = new Coord(0, 0, 0);
		var enemy = battle.Engine.World.Units.Values.First(unit => unit.Controller == EController.Enemy);
		enemy.State.Position = new Coord(5, 5, 0);

		var envelope = TorpedoReachEnvelope.Build(battle.Engine.CreateSimulation(), torpedoId);

		Assert.False(envelope.WithinBlast(0, enemy.State.Position));
	}

	[Fact]
	public void WithinBlast_FutureLayerCanCoverFartherAhead()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		FaceForward(battle, torpedoId);
		var start = new Coord(5, 5, 1);
		battle.Engine.World.StateOf(torpedoId).Position = start;
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = 3;
		battle.Engine.World.StateOf(torpedoId).MomentumLevel = 0;
		battle.Engine.World.StateOf(PlayerId).Position = new Coord(0, 0, 0);
		var enemy = battle.Engine.World.Units.Values.First(unit => unit.Controller == EController.Enemy);
		enemy.State.Position = new Coord(1, 1, 1);
		var farAhead = start + Coord.Forward * 10;

		var envelope = TorpedoReachEnvelope.Build(battle.Engine.CreateSimulation(), torpedoId);

		Assert.False(envelope.WithinBlast(0, farAhead));
		Assert.True(
			envelope.WithinBlast(1, farAhead) || envelope.WithinBlast(2, farAhead),
			"farther target should enter blast on a future fuel activation");
	}

	private static void FaceForward(Battle.BattleOrchestrator battle, string torpedoId)
	{
		var state = battle.Engine.World.StateOf(torpedoId);
		state.Fore = Coord.Forward;
		state.Dorsal = Coord.Up;
		state.Starboard = Coord.Cross(Coord.Up, Coord.Forward);
	}

	private static Battle.BattleOrchestrator BattleWithTorpedo(out string torpedoId)
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		battle.Engine.Commit(new TorpedoAction(PlayerId, ETorpedoMount.Aft));
		var torpedo = Assert.Single(battle.Engine.World.Units.Values, unit => unit.State.Type == EType.Torpedo);
		torpedoId = torpedo.State.Id;
		return battle;
	}
}
