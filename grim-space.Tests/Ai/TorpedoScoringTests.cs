using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Actions;
using GrimSpace.Units.Enums;
using GrimSpace.Battle.Units;

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
		var session = battle.Engine.CreateSimulation();
		TorpedoExecutionAgent.Instance.Plan(torpedo, session);

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
		var session = battle.Engine.CreateSimulation();
		var actions = TorpedoExecutionAgent.Instance.Plan(torpedo, session);

		Assert.Contains(actions, action => action is DetonateAction);
		Assert.True(
			session.StateOf<ActorState>(torpedoId).Position.ManhattanDistanceTo(enemy.State.Position)
			<= TorpedoConfig.BlastRadius);
		Assert.False(session.StateOf<ActorState>(torpedoId).IsAlive);
	}

	[Fact]
	public void Plan_PrefersOpponentBlastOverDeepForwardAway()
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
		enemy.State.Position = torpedoPos + new Coord(1, 0, 0);

		var torpedo = UnitRegistry.For(battle.Engine.World).UnitOf(torpedoId);
		var session = battle.Engine.CreateSimulation();
		var actions = TorpedoExecutionAgent.Instance.Plan(torpedo, session);

		Assert.Contains(actions, action => action is DetonateAction);
		Assert.True(
			session.StateOf<ActorState>(torpedoId).Position.ManhattanDistanceTo(enemy.State.Position)
			<= TorpedoConfig.BlastRadius);
	}

	private static BattleOrchestrator BattleWithTorpedo(out string torpedoId)
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		battle.Engine.Commit(new TorpedoAction(PlayerId, ETorpedoMount.Aft));
		var torpedo = Assert.Single(UnitRegistry.For(battle.Engine.World).All, unit => unit.State.Type == EType.Torpedo);
		torpedoId = torpedo.State.Id;
		return battle;
	}
}
