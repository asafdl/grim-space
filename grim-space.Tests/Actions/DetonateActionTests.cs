using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

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
	public void DetonateDamagesUnitsInBlastIncludingFriendlyAndKillsTorpedo()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		var torpedoPos = new Coord(5, 5, 5);
		battle.Engine.World.StateOf(torpedoId).Position = torpedoPos;
		battle.Engine.World.StateOf(PlayerId).Position = torpedoPos + new Coord(1, 0, 0);
		var enemy = battle.Engine.World.Units.Values.First(unit => unit.Controller == EController.Enemy);
		enemy.State.Position = torpedoPos + new Coord(0, 1, 0);

		var playerShieldsBefore = TotalShields(battle.Engine.World.StateOf(PlayerId));
		var enemyShieldsBefore = TotalShields(enemy.State);

		var sim = battle.Engine.CreateSimulation();
		Assert.True(sim.TryEnqueue(new DetonateAction(torpedoId)));

		Assert.True(sim.World.Units.ContainsKey(torpedoId));
		Assert.False(sim.StateOf<ActorState>(torpedoId).IsAlive);
		Assert.True(TotalShields(sim.StateOf<ActorState>(PlayerId)) < playerShieldsBefore
			|| sim.StateOf<ActorState>(PlayerId).HullPoints < battle.Engine.World.StateOf(PlayerId).HullPoints);
		Assert.True(TotalShields(sim.StateOf<ActorState>(enemy.State.Id)) < enemyShieldsBefore
			|| sim.StateOf<ActorState>(enemy.State.Id).HullPoints < enemy.State.HullPoints);
	}

	[Fact]
	public async Task AgentBurnsFuelAndDetonatesWhenFuelAlreadyZero()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		PlaceFarFromEveryone(battle, torpedoId);
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = 0;
		var torpedo = battle.Engine.World.UnitOf(torpedoId);

		var actions = await torpedo.ExecutionAgent.GetActionsAsync(torpedo, battle.Engine.CreateSimulation);

		Assert.Contains(actions, action => action is FuelBurnAction);
		Assert.Contains(actions, action => action is DetonateAction);
	}

	[Fact]
	public async Task AgentDetonatesWhenUnitInBlastAfterMoves()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		var torpedoPos = new Coord(5, 5, 5);
		battle.Engine.World.StateOf(torpedoId).Position = torpedoPos;
		battle.Engine.World.StateOf(torpedoId).ActionPoints = 0;
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = TorpedoConfig.Fuel;
		battle.Engine.World.StateOf(PlayerId).Position = torpedoPos + new Coord(1, 0, 0);
		var torpedo = battle.Engine.World.UnitOf(torpedoId);

		var actions = await torpedo.ExecutionAgent.GetActionsAsync(torpedo, battle.Engine.CreateSimulation);

		Assert.Contains(actions, action => action is FuelBurnAction);
		Assert.Contains(actions, action => action is DetonateAction);
	}

	[Fact]
	public void ResolveTurnKillsTorpedoOnForcedDetonate()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		PlaceFarFromEveryone(battle, torpedoId);
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = 0;
		battle.BeginTurn();

		var replay = battle.ResolveTurn();

		Assert.Contains(replay.Actions, action => action is DetonateAction);
		Assert.True(battle.Engine.World.Units.ContainsKey(torpedoId));
		Assert.False(battle.Engine.World.StateOf(torpedoId).IsAlive);
	}

	private static BattleOrchestrator BattleWithTorpedo(out string torpedoId)
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		battle.Engine.Commit(new TorpedoAction(PlayerId, ETorpedoMount.Aft));
		var torpedo = Assert.Single(battle.Engine.World.Units.Values, unit => unit.State.Type == EType.Torpedo);
		torpedoId = torpedo.State.Id;
		battle.BeginTurn();
		return battle;
	}

	private static void PlaceFarFromEveryone(BattleOrchestrator battle, string torpedoId)
	{
		battle.Engine.World.StateOf(torpedoId).Position = new Coord(10, 10, 10);
		battle.Engine.World.StateOf(PlayerId).Position = new Coord(1, 1, 1);
		var enemy = battle.Engine.World.Units.Values.First(unit => unit.Controller == EController.Enemy);
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
