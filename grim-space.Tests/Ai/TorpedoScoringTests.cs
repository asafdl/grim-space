using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Actions;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Ai;

public sealed class TorpedoScoringTests
{
	private const string PlayerId = "player";

	[Fact]
	public void Plan_ClosesOnOpponentAhead()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		var torpedoPos = new Coord(5, 5, 3);
		battle.Engine.World.StateOf(torpedoId).Position = torpedoPos;
		battle.Engine.World.StateOf(torpedoId).Fore = Coord.Forward;
		battle.Engine.World.StateOf(PlayerId).Position = new Coord(0, 0, 0);
		var enemy = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.Alliance.Team == ETeam.Enemy);
		enemy.State.Position = new Coord(5, 5, 9);

		var startDistance = torpedoPos.ManhattanDistanceTo(enemy.State.Position);
		var torpedo = UnitRegistry.For(battle.Engine.World).UnitOf(torpedoId);
		var agent = (TorpedoExecutionAgent)torpedo.ExecutionAgent;
		var session = battle.Engine.CreateSimulation();
		agent.Plan(torpedo, session);

		var endDistance = session.StateOf<ActorState>(torpedoId).Position
			.ManhattanDistanceTo(enemy.State.Position);
		Assert.True(endDistance < startDistance);
	}

	[Fact]
	public void Plan_EntersBlastAndDetonatesWhenFuelExpires()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		var torpedoPos = new Coord(5, 5, 5);
		battle.Engine.World.StateOf(torpedoId).Position = torpedoPos;
		battle.Engine.World.StateOf(torpedoId).Fore = Coord.Forward;
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = 1;
		battle.Engine.World.StateOf(PlayerId).Position = new Coord(0, 0, 0);
		var enemy = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.Alliance.Team == ETeam.Enemy);
		enemy.State.Position = torpedoPos + Coord.Forward * (TorpedoConfig.BlastRadius + 1);

		var torpedo = UnitRegistry.For(battle.Engine.World).UnitOf(torpedoId);
		var agent = (TorpedoExecutionAgent)torpedo.ExecutionAgent;
		var session = battle.Engine.CreateSimulation();
		var actions = agent.Plan(torpedo, session);

		Assert.Contains(actions, action => action is DetonateAction);
		Assert.True(
			session.StateOf<ActorState>(torpedoId).Position.ManhattanDistanceTo(enemy.State.Position)
			<= TorpedoConfig.BlastRadius);
		Assert.False(session.StateOf<ActorState>(torpedoId).IsAlive);
	}

	[Fact]
	public void Plan_DetonatesWithoutMovingWhenOpponentAlreadyInBlast()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		var torpedoPos = new Coord(5, 5, 5);
		battle.Engine.World.StateOf(torpedoId).Position = torpedoPos;
		battle.Engine.World.StateOf(torpedoId).Fore = Coord.Forward;
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = TorpedoConfig.Fuel;
		battle.Engine.World.StateOf(PlayerId).Position = new Coord(0, 0, 0);
		var enemy = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.Alliance.Team == ETeam.Enemy);
		enemy.State.Position = torpedoPos + new Coord(1, 0, 0);

		var torpedo = UnitRegistry.For(battle.Engine.World).UnitOf(torpedoId);
		var agent = (TorpedoExecutionAgent)torpedo.ExecutionAgent;
		var session = battle.Engine.CreateSimulation();
		var actions = agent.Plan(torpedo, session);

		Assert.DoesNotContain(actions, action => action is MoveStepAction);
		Assert.Contains(actions, action => action is DetonateAction);
	}

	[Fact]
	public void Plan_DetonatesOnBehindOpponentWhenOnlyReachableThreat()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		var torpedoPos = new Coord(5, 5, 5);
		battle.Engine.World.StateOf(torpedoId).Position = torpedoPos;
		battle.Engine.World.StateOf(torpedoId).Fore = Coord.Forward;
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = TorpedoConfig.Fuel;
		battle.Engine.World.StateOf(PlayerId).Position = new Coord(0, 0, 0);
		var enemy = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.Alliance.Team == ETeam.Enemy);
		enemy.State.Position = torpedoPos + Coord.Forward * -2;

		var torpedo = UnitRegistry.For(battle.Engine.World).UnitOf(torpedoId);
		var agent = (TorpedoExecutionAgent)torpedo.ExecutionAgent;
		var session = battle.Engine.CreateSimulation();
		var actions = agent.Plan(torpedo, session);

		Assert.DoesNotContain(actions, action => action is MoveStepAction);
		Assert.Contains(actions, action => action is DetonateAction);
	}

	[Fact]
	public void Plan_ChasesAheadWhileBehindOpponentRemainsInBlast()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		var torpedoPos = new Coord(5, 5, 5);
		battle.Engine.World.StateOf(torpedoId).Position = torpedoPos;
		battle.Engine.World.StateOf(torpedoId).Fore = Coord.Forward;
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = TorpedoConfig.Fuel;
		battle.Engine.World.StateOf(PlayerId).Position = new Coord(0, 0, 0);

		var ahead = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.Alliance.Team == ETeam.Enemy);
		ahead.State.Position = torpedoPos + Coord.Forward * 6;

		var behind = Factory.Create(
			new Instance
			{
				Id = "behind",
				Type = EType.Carrier,
				Alliance = Alliance.Enemy,
			},
			torpedoPos + Coord.Forward * -2,
			new AiController());
		UnitRegistry.For(battle.Engine.World).Add(behind);

		var torpedo = UnitRegistry.For(battle.Engine.World).UnitOf(torpedoId);
		var agent = (TorpedoExecutionAgent)torpedo.ExecutionAgent;
		var session = battle.Engine.CreateSimulation();
		agent.Plan(torpedo, session);

		var end = session.StateOf<ActorState>(torpedoId).Position;
		Assert.True(end.Z > torpedoPos.Z);
		Assert.True(end.ManhattanDistanceTo(ahead.State.Position) < torpedoPos.ManhattanDistanceTo(ahead.State.Position));
	}

	[Fact]
	public void Plan_PrefersDetonateOverChargingPastOpponent()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		var torpedoPos = new Coord(5, 5, 5);
		battle.Engine.World.StateOf(torpedoId).Position = torpedoPos;
		battle.Engine.World.StateOf(torpedoId).Fore = Coord.Forward;
		battle.Engine.World.StateOf(torpedoId).Dorsal = Coord.Up;
		battle.Engine.World.StateOf(torpedoId).Starboard = Coord.Cross(Coord.Up, Coord.Forward);
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = TorpedoConfig.Fuel;
		battle.Engine.World.StateOf(PlayerId).Position = new Coord(0, 0, 0);
		var enemy = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.Alliance.Team == ETeam.Enemy);
		enemy.State.Position = torpedoPos + Coord.Forward * 2;

		var torpedo = UnitRegistry.For(battle.Engine.World).UnitOf(torpedoId);
		var agent = (TorpedoExecutionAgent)torpedo.ExecutionAgent;
		var session = battle.Engine.CreateSimulation();
		var actions = agent.Plan(torpedo, session);

		Assert.Contains(actions, action => action is DetonateAction);
		Assert.True(
			session.StateOf<ActorState>(torpedoId).Position.ManhattanDistanceTo(enemy.State.Position)
			<= TorpedoConfig.BlastRadius);
		Assert.True(session.StateOf<ActorState>(torpedoId).Position.Z <= torpedoPos.Z + 1);
	}

	private static BattleOrchestrator BattleWithTorpedo(out string torpedoId)
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		battle.Engine.Commit(new TorpedoAction(PlayerId, ESpatialOrientation.Retro));
		var torpedo = Assert.Single(UnitRegistry.For(battle.Engine.World).All, unit => unit.State.Type == EType.Torpedo);
		torpedoId = torpedo.State.Id;
		return battle;
	}
}
