using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Actions;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Ai;

[BattleTestSuite]
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
		var enemy = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.Team == ETeam.Enemy);
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
		var enemy = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.Team == ETeam.Enemy);
		enemy.State.Position = torpedoPos + Coord.Forward * (CatalogExpectations.DefaultTorpedoLauncher().BlastRadius + 1);

		var torpedo = UnitRegistry.For(battle.Engine.World).UnitOf(torpedoId);
		var agent = (TorpedoExecutionAgent)torpedo.ExecutionAgent;
		var session = battle.Engine.CreateSimulation();
		var actions = agent.Plan(torpedo, session);

		Assert.Contains(actions, action => action is DetonateAction);
		Assert.True(
			session.StateOf<ActorState>(torpedoId).Position.ManhattanDistanceTo(enemy.State.Position)
			<= CatalogExpectations.DefaultTorpedoLauncher().BlastRadius);
		Assert.False(session.StateOf<ActorState>(torpedoId).IsAlive);
	}

	[Fact]
	public void Plan_DetonatesWithoutMovingWhenOpponentAlreadyInBlast()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		var torpedoPos = new Coord(5, 5, 5);
		battle.Engine.World.StateOf(torpedoId).Position = torpedoPos;
		battle.Engine.World.StateOf(torpedoId).Fore = Coord.Forward;
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = CatalogExpectations.DefaultTorpedoLauncher().FuelTurns;
		battle.Engine.World.StateOf(PlayerId).Position = new Coord(0, 0, 0);
		var enemy = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.Team == ETeam.Enemy);
		enemy.State.Position = torpedoPos + new Coord(1, 0, 0);

		var torpedo = UnitRegistry.For(battle.Engine.World).UnitOf(torpedoId);
		var agent = (TorpedoExecutionAgent)torpedo.ExecutionAgent;
		var session = battle.Engine.CreateSimulation();
		var actions = agent.Plan(torpedo, session);

		Assert.DoesNotContain(actions, action => action is MoveStepAction);
		Assert.DoesNotContain(actions, action => action is FuelBurnAction);
		Assert.Contains(actions, action => action is DetonateAction);
	}

	[Fact]
	public void Plan_DetonatesOnBehindOpponentWhenOnlyReachableThreat()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		var torpedoPos = new Coord(5, 5, 5);
		battle.Engine.World.StateOf(torpedoId).Position = torpedoPos;
		battle.Engine.World.StateOf(torpedoId).Fore = Coord.Forward;
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = CatalogExpectations.DefaultTorpedoLauncher().FuelTurns;
		battle.Engine.World.StateOf(PlayerId).Position = new Coord(0, 0, 0);
		var enemy = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.Team == ETeam.Enemy);
		enemy.State.Position = torpedoPos + Coord.Forward * -2;

		var torpedo = UnitRegistry.For(battle.Engine.World).UnitOf(torpedoId);
		var agent = (TorpedoExecutionAgent)torpedo.ExecutionAgent;
		var session = battle.Engine.CreateSimulation();
		var actions = agent.Plan(torpedo, session);

		Assert.DoesNotContain(actions, action => action is MoveStepAction);
		Assert.DoesNotContain(actions, action => action is FuelBurnAction);
		Assert.Contains(actions, action => action is DetonateAction);
	}

	[Fact]
	public void Plan_DetonatesOnBehindOpponentInsteadOfChasingAhead()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		var torpedoPos = new Coord(5, 5, 5);
		battle.Engine.World.StateOf(torpedoId).Position = torpedoPos;
		battle.Engine.World.StateOf(torpedoId).Fore = Coord.Forward;
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = CatalogExpectations.DefaultTorpedoLauncher().FuelTurns;
		battle.Engine.World.StateOf(PlayerId).Position = new Coord(0, 0, 0);

		var ahead = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.Team == ETeam.Enemy);
		ahead.State.Position = torpedoPos + Coord.Forward * 6;

		var behind = Factory.Create(
			ShipInstance.FromCatalog("behind", EType.Carrier),
			ETeam.Enemy,
			torpedoPos + Coord.Forward * -2,
			new AiController());
		UnitRegistry.For(battle.Engine.World).Add(behind);

		var torpedo = UnitRegistry.For(battle.Engine.World).UnitOf(torpedoId);
		var agent = (TorpedoExecutionAgent)torpedo.ExecutionAgent;
		var session = battle.Engine.CreateSimulation();
		var actions = agent.Plan(torpedo, session);

		Assert.DoesNotContain(actions, action => action is MoveStepAction);
		Assert.Contains(actions, action => action is DetonateAction);
		Assert.False(session.StateOf<ActorState>(torpedoId).IsAlive);
	}

	[Fact]
	public void Plan_PrefersCleanDetonationOverEarlierCollateralShot()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		var torpedoPos = new Coord(5, 5, 5);
		battle.Engine.World.StateOf(torpedoId).Position = torpedoPos;
		battle.Engine.World.StateOf(torpedoId).Fore = Coord.Forward;
		battle.Engine.World.StateOf(torpedoId).Dorsal = Coord.Up;
		battle.Engine.World.StateOf(torpedoId).Starboard = Coord.Cross(Coord.Up, Coord.Forward);
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = CatalogExpectations.DefaultTorpedoLauncher().FuelTurns;

		var ally = battle.Engine.World.StateOf(PlayerId);
		ally.Position = new Coord(1, 5, 6);
		var enemy = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.Team == ETeam.Enemy);
		enemy.State.Position = new Coord(5, 5, 10);

		var torpedo = UnitRegistry.For(battle.Engine.World).UnitOf(torpedoId);
		var session = battle.Engine.CreateSimulation();
		var actions = ((TorpedoExecutionAgent)torpedo.ExecutionAgent).Plan(torpedo, session);
		var detonationPosition = session.StateOf<ActorState>(torpedoId).Position;

		Assert.Contains(actions, action => action is DetonateAction);
		Assert.True(detonationPosition.ManhattanDistanceTo(enemy.State.Position) <= CatalogExpectations.DefaultTorpedoLauncher().BlastRadius);
		Assert.True(detonationPosition.ManhattanDistanceTo(ally.Position) > CatalogExpectations.DefaultTorpedoLauncher().BlastRadius);
	}

	[Fact]
	public void Plan_PrefersCleanDetonationOverImmediateCollateral()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		var torpedoPos = new Coord(5, 5, 5);
		battle.Engine.World.StateOf(torpedoId).Position = torpedoPos;
		battle.Engine.World.StateOf(torpedoId).Fore = Coord.Forward;
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = CatalogExpectations.DefaultTorpedoLauncher().FuelTurns;
		battle.Engine.World.StateOf(PlayerId).Position = new Coord(4, 4, 5);

		var enemy = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.Team == ETeam.Enemy);
		enemy.State.Position = torpedoPos + Coord.Forward * 4;

		var torpedo = UnitRegistry.For(battle.Engine.World).UnitOf(torpedoId);
		var agent = (TorpedoExecutionAgent)torpedo.ExecutionAgent;
		var session = battle.Engine.CreateSimulation();
		agent.Plan(torpedo, session);

		var end = session.StateOf<ActorState>(torpedoId).Position;
		var playerPos = session.StateOf<ActorState>(PlayerId).Position;
		Assert.True(end.Z > torpedoPos.Z);
		Assert.True(end.ManhattanDistanceTo(playerPos) > CatalogExpectations.DefaultTorpedoLauncher().BlastRadius);
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
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = CatalogExpectations.DefaultTorpedoLauncher().FuelTurns;
		battle.Engine.World.StateOf(PlayerId).Position = new Coord(0, 0, 0);
		var enemy = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.Team == ETeam.Enemy);
		enemy.State.Position = torpedoPos + Coord.Forward * 2;

		var torpedo = UnitRegistry.For(battle.Engine.World).UnitOf(torpedoId);
		var agent = (TorpedoExecutionAgent)torpedo.ExecutionAgent;
		var session = battle.Engine.CreateSimulation();
		var actions = agent.Plan(torpedo, session);

		Assert.Contains(actions, action => action is DetonateAction);
		Assert.True(
			session.StateOf<ActorState>(torpedoId).Position.ManhattanDistanceTo(enemy.State.Position)
			<= CatalogExpectations.DefaultTorpedoLauncher().BlastRadius);
		Assert.True(session.StateOf<ActorState>(torpedoId).Position.Z <= torpedoPos.Z + 1);
	}

	[Fact]
	public void Plan_PrefersFourForwardStepsWithoutReachableTarget()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		var torpedoPos = new Coord(5, 5, 5);
		var state = battle.Engine.World.StateOf(torpedoId);
		state.Position = torpedoPos;
		state.Fore = Coord.Forward;
		state.Dorsal = Coord.Up;
		state.Starboard = Coord.Cross(Coord.Up, Coord.Forward);
		battle.Engine.World.StateOf(PlayerId).Position = new Coord(0, 0, 0);
		var enemy = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.Team == ETeam.Enemy);
		enemy.State.Position = torpedoPos - Coord.Forward * 5;

		var torpedo = UnitRegistry.For(battle.Engine.World).UnitOf(torpedoId);
		var actions = ((TorpedoExecutionAgent)torpedo.ExecutionAgent)
			.Plan(torpedo, battle.Engine.CreateSimulation());
		var moves = actions.OfType<TorpedoMoveStepAction>().ToList();

		Assert.Equal(CatalogExpectations.DefaultTorpedoLauncher().MovementActionPoints, moves.Count);
		Assert.All(moves, move => Assert.Equal(ESpatialOrientation.Forward, move.Direction));
	}

	[Fact]
	public void Plan_UsesLateralMovementToReachBlastRange()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		var torpedoPos = new Coord(5, 5, 5);
		var state = battle.Engine.World.StateOf(torpedoId);
		state.Position = torpedoPos;
		state.Fore = Coord.Forward;
		state.Dorsal = Coord.Up;
		state.Starboard = Coord.Cross(Coord.Up, Coord.Forward);
		battle.Engine.World.StateOf(PlayerId).Position = new Coord(0, 0, 0);
		var enemy = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.Team == ETeam.Enemy);
		enemy.State.Position = torpedoPos + state.Starboard * (CatalogExpectations.DefaultTorpedoLauncher().BlastRadius + 1);

		var torpedo = UnitRegistry.For(battle.Engine.World).UnitOf(torpedoId);
		var actions = ((TorpedoExecutionAgent)torpedo.ExecutionAgent)
			.Plan(torpedo, battle.Engine.CreateSimulation());

		Assert.Contains(
			actions,
			action => action is TorpedoMoveStepAction
			{
				Direction: ESpatialOrientation.Starboard
			});
		Assert.Contains(actions, action => action is DetonateAction);
	}

	private static BattleOrchestrator BattleWithTorpedo(out string torpedoId)
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		battle.Engine.Commit(TorpedoDef.Instance.Bind(PlayerId, ESpatialOrientation.Retro));
		var torpedo = Assert.Single(UnitRegistry.For(battle.Engine.World).All, unit => unit.State.Type == EType.Torpedo);
		torpedoId = torpedo.State.Id;
		return battle;
	}
}
