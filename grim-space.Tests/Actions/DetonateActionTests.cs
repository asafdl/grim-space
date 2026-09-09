using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Battle.Units;

namespace GrimSpace.Tests.Actions;

public sealed class DetonateActionTests
{
	private const string PlayerId = "player";

	[Fact]
	public void FuelBurnDecrementsFuel()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		var fuelBefore = battle.Engine.World.StateOf(torpedoId).FuelRemaining;

		var sim = battle.Engine.CreateSimulation();
		Assert.True(sim.TryEnqueue(new FuelBurnAction(torpedoId)));
		Assert.Equal(fuelBefore - 1, sim.StateOf<ActorState>(torpedoId).FuelRemaining);
		Assert.Equal(fuelBefore, battle.Engine.World.StateOf(torpedoId).FuelRemaining);
	}

	[Fact]
	public void DetonateIllegalWhenFuelRemainsAndBlastEmpty()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		PlaceFarFromEveryone(battle, torpedoId);

		var sim = battle.Engine.CreateSimulation();
		Assert.False(sim.TryEnqueue(new DetonateAction(torpedoId)));
	}

	[Fact]
	public void DetonateIllegalWhenFuelRemainsAndOnlyAllyInBlast()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		var torpedoPos = new Coord(5, 5, 5);
		battle.Engine.World.StateOf(torpedoId).Position = torpedoPos;
		battle.Engine.World.StateOf(PlayerId).Position = torpedoPos + new Coord(1, 0, 0);
		var enemy = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.Alliance.Team == ETeam.Enemy);
		enemy.State.Position = new Coord(0, 0, 0);

		var sim = battle.Engine.CreateSimulation();
		Assert.False(sim.TryEnqueue(new DetonateAction(torpedoId)));
	}

	[Fact]
	public void DetonateLegalWhenFuelExhausted()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		PlaceFarFromEveryone(battle, torpedoId);
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = 0;

		var sim = battle.Engine.CreateSimulation();
		Assert.True(sim.TryEnqueue(new DetonateAction(torpedoId)));
		Assert.False(sim.StateOf<ActorState>(torpedoId).IsAlive);
	}

	[Fact]
	public void DetonateResolutionDamagesEveryUnitInBlastAndKillsTorpedo()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		var torpedoPos = new Coord(5, 5, 5);
		battle.Engine.World.StateOf(torpedoId).Position = torpedoPos;
		battle.Engine.World.StateOf(PlayerId).Position = torpedoPos + new Coord(1, 0, 0);
		var enemy = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.Alliance.Team == ETeam.Enemy);
		enemy.State.Position = torpedoPos + new Coord(0, 1, 0);

		var playerShieldsBefore = TotalShields(battle.Engine.World.StateOf(PlayerId));
		var enemyShieldsBefore = TotalShields(enemy.State);

		var sim = battle.Engine.CreateSimulation();
		Assert.True(sim.TryEnqueue(new DetonateAction(torpedoId)));

		Assert.True(UnitRegistry.For(sim.World).Contains(torpedoId));
		Assert.False(sim.StateOf<ActorState>(torpedoId).IsAlive);
		Assert.True(TotalShields(sim.StateOf<ActorState>(PlayerId)) < playerShieldsBefore
			|| sim.StateOf<ActorState>(PlayerId).HullPoints < battle.Engine.World.StateOf(PlayerId).HullPoints);
		Assert.True(TotalShields(sim.StateOf<ActorState>(enemy.State.Id)) < enemyShieldsBefore
			|| sim.StateOf<ActorState>(enemy.State.Id).HullPoints < enemy.State.HullPoints);
	}

	[Fact]
	public async Task AgentDetonatesWithoutMovingOrBurningFuelWhenFuelAlreadyZero()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		PlaceFarFromEveryone(battle, torpedoId);
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = 0;
		var torpedo = UnitRegistry.For(battle.Engine.World).UnitOf(torpedoId);
		var actions = await BattleTestFixture.AwaitUnitActions(battle, torpedo);

		Assert.DoesNotContain(actions, action => action is MoveStepAction);
		Assert.DoesNotContain(actions, action => action is FuelBurnAction);
		Assert.Contains(actions, action => action is DetonateAction);
	}

	[Fact]
	public async Task AgentDetonatesImmediatelyWhenOpponentInBlast()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		var torpedoPos = new Coord(5, 5, 5);
		battle.Engine.World.StateOf(torpedoId).Position = torpedoPos;
		battle.Engine.World.StateOf(torpedoId).ActionPoints = 0;
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = TorpedoConfig.Fuel;
		battle.Engine.World.StateOf(PlayerId).Position = new Coord(0, 0, 0);
		var enemy = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.Alliance.Team == ETeam.Enemy);
		enemy.State.Position = torpedoPos + new Coord(1, 0, 0);
		var torpedo = UnitRegistry.For(battle.Engine.World).UnitOf(torpedoId);
		var actions = await BattleTestFixture.AwaitUnitActions(battle, torpedo);

		Assert.DoesNotContain(actions, action => action is MoveStepAction);
		Assert.DoesNotContain(actions, action => action is FuelBurnAction);
		Assert.Contains(actions, action => action is DetonateAction);
	}

	[Fact]
	public void ResolveTurnKillsTorpedoOnForcedDetonate()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		PlaceFarFromEveryone(battle, torpedoId);
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = 0;
		BattleTestFixture.ResetPlayerPlanning(battle);
		var replay = BattleTestActions.CommitAndResolve(battle);

		Assert.Contains(replay.Actions, action => action is DetonateAction);
		Assert.True(UnitRegistry.For(battle.Engine.World).Contains(torpedoId));
		Assert.False(battle.Engine.World.StateOf(torpedoId).IsAlive);
	}

	private static BattleOrchestrator BattleWithTorpedo(out string torpedoId)
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		battle.Engine.Commit(TorpedoDef.Instance.Bind(PlayerId, ESpatialOrientation.Retro));
		var torpedo = Assert.Single(UnitRegistry.For(battle.Engine.World).All, unit => unit.State.Type == EType.Torpedo);
		torpedoId = torpedo.State.Id;
		ExecutionAgent<BattleWorld, ActorRuntime>.Initialize(
			torpedo.ExecutionAgent,
			torpedoId,
			battle.Engine.CreateSimulation,
			battle.WriterFor(torpedoId));
		BattleTestFixture.ResetPlayerPlanning(battle);
		return battle;
	}

	private static void PlaceFarFromEveryone(BattleOrchestrator battle, string torpedoId)
	{
		battle.Engine.World.StateOf(torpedoId).Position = new Coord(10, 10, 10);
		battle.Engine.World.StateOf(PlayerId).Position = new Coord(1, 1, 1);
		var enemy = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.Alliance.Team == ETeam.Enemy);
		enemy.State.Position = new Coord(0, 0, 0);
	}

	private static int TotalShields(GrimSpace.Battle.Units.State state)
	{
		var total = 0;
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			total += state.ShieldPoints[face];
		return total;
	}
}
