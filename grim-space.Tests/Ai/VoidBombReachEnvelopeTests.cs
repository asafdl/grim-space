using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Abilities;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Actions;
using GrimSpace.Tests.Dfs;
using GrimSpace.Units.Enums;
using GrimSpace.Battle.Units;

namespace GrimSpace.Tests.Ai;

[BattleTestSuite]
public sealed class VoidBombReachEnvelopeTests
{
	private const string PlayerId = "player";

	[Fact]
	public void Build_HasOneLayerPerFuel()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		FaceForward(battle, torpedoId);
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = 2;

		var envelope = VoidBombReachEnvelope.Build(battle.Engine.CreateSimulation(), torpedoId);

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
		torpedo.CurrentActionPoints = torpedo.Maneuverability.BaseActionPoints;

		var envelope = VoidBombReachEnvelope.Build(battle.Engine.CreateSimulation(), torpedoId);
		var start = torpedo.Position;

		Assert.Contains(start, envelope.Layers[0]);
		Assert.True(envelope.Layers[0].Count > 1);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Build_FirstLayerMatchesExhaustiveReachWithoutChangingQueuedPrefix(bool queuedMove)
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		FaceForward(battle, torpedoId);
		var state = battle.Engine.World.StateOf(torpedoId);
		state.FuelRemaining = 1;
		state.CurrentActionPoints = 2;
		var session = battle.Engine.CreateSimulation();
		if (queuedMove)
			Assert.True(session.TryEnqueue(new VoidBombMoveStepAction(torpedoId, ESpatialOrientation.Forward)));
		var prefix = session.Actions.ToArray();
		var start = session.World.StateOf(torpedoId).Position;
		var ap = session.World.StateOf(torpedoId).CurrentActionPoints;
		var expected = ActionSearchExhaustive.Run(session, torpedoId, new[] { VoidBombMoveDef.Instance })
			.Select(frame => frame.World.StateOf(torpedoId).Position)
			.ToHashSet();

		var envelope = VoidBombReachEnvelope.Build(session, torpedoId);

		Assert.True(Assert.Single(envelope.Layers).SetEquals(expected));
		Assert.Equal(prefix, session.Actions);
		Assert.Equal(start, session.World.StateOf(torpedoId).Position);
		Assert.Equal(ap, session.World.StateOf(torpedoId).CurrentActionPoints);
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

		var envelope = VoidBombReachEnvelope.Build(battle.Engine.CreateSimulation(), torpedoId);

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
		battle.Engine.World.StateOf(PlayerId).Position = new Coord(0, 0, 0);
		var enemy = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.Team == ETeam.Enemy);
		enemy.State.Position = new Coord(5, 5, 0);

		var envelope = VoidBombReachEnvelope.Build(battle.Engine.CreateSimulation(), torpedoId);

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
		battle.Engine.World.StateOf(PlayerId).Position = new Coord(0, 0, 0);
		var enemy = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.Team == ETeam.Enemy);
		enemy.State.Position = new Coord(1, 1, 1);
		var farAhead = start + Coord.Forward * 10;

		var envelope = VoidBombReachEnvelope.Build(battle.Engine.CreateSimulation(), torpedoId);

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
		battle.Engine.Commit(VoidBombDef.Instance.Bind(PlayerId, ESpatialOrientation.Ventral));
		var torpedo = Assert.Single(UnitRegistry.For(battle.Engine.World).All, unit => unit.State.Type == EType.VoidBomb);
		torpedoId = torpedo.State.Id;
		return battle;
	}
}
