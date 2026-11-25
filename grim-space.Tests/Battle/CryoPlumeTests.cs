using GrimSpace.Battle;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Run.Persistence;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.Factions;

namespace GrimSpace.Tests.Hazards;

[BattleTestSuite]
public sealed class CryoPlumeTests
{
	private const string PlayerId = "player";
	private static readonly Coord Origin = new(5, 5, 5);

	[Fact]
	public void EnteringPlumeCoolsOncePerPlumePerTurnAndUndoRestores()
	{
		var battle = BattleTestFixture.BeginSimulation(Origin);
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		var state = world.StateOf(PlayerId);
		var first = AddPlume(battle, "cryoplume-a", Origin + Coord.Up);
		var second = AddPlume(battle, "cryoplume-b", Origin + Coord.Up * 2);
		state.Heat = 80;

		var firstEntry = new MoveEffect(first).Apply(world, runtime, PlayerId);
		Assert.Contains(firstEntry, record => record is GrimSpace.Core.Actions.Record<CryoPlumeCooledFacts> { Value.PlumeId: "cryoplume-a" });
		new MoveEffect(Origin).Apply(world, runtime, PlayerId);
		var reentry = new MoveEffect(first);
		reentry.Apply(world, runtime, PlayerId);
		Assert.Equal(80 - CryoPlume.HeatCleared, state.Heat);

		var other = new MoveEffect(second);
		other.Apply(world, runtime, PlayerId);
		Assert.Equal(80 - 2 * CryoPlume.HeatCleared, state.Heat);

		other.Undo(world, runtime, PlayerId);
		Assert.Equal(80 - CryoPlume.HeatCleared, state.Heat);
		Assert.DoesNotContain("cryoplume-b", state.CryoPlumesCooledThisTurn);
	}

	[Fact]
	public void MovePreviewAppliesPlumeCoolingWhenEnteringOrPassingThrough()
	{
		var grid = BattleTestFixture.Grid(20);
		var player = BattleTestFixture.Player(Origin);
		player.State.Heat = 80;
		var plume = Origin + player.State.Fore;
		var battle = BattleTestFixture.BeginSimulation(
			player,
			[BattleTestFixture.Enemy(Coord.Zero)],
			grid,
			hazards: [CryoPlume.Create("cryoplume-a", plume, grid)]);
		var paths = MovePathEndpoints.DiscoverExtensions(battle.PlayerAgent.Sim, PlayerId);

		Assert.Equal(
			80 + HeatChangeEffect.PerApSpent - CryoPlume.HeatCleared,
			paths.First(path => path.EndPosition == plume).ResultState.Heat);
		Assert.Equal(
			80 + 2 * HeatChangeEffect.PerApSpent - CryoPlume.HeatCleared,
			paths.First(path => path.EndPosition == plume + player.State.Fore).ResultState.Heat);
	}

	[Fact]
	public void UpkeepInPlumeClearsExtraHeatAndResetsPassThroughs()
	{
		var battle = BattleTestFixture.BeginSimulation(Origin);
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		var state = world.StateOf(PlayerId);
		AddPlume(battle, "cryoplume-a", Origin);
		state.Heat = 80;
		state.CryoPlumesCooledThisTurn.Add("cryoplume-a");

		var upkeep = new TurnUpkeepEffect();
		upkeep.Apply(world, runtime, PlayerId);
		Assert.Equal(80 - state.HeatProfile.ClearPerTurn - CryoPlume.HeatCleared, state.Heat);
		Assert.Empty(state.CryoPlumesCooledThisTurn);

		upkeep.Undo(world, runtime, PlayerId);
		Assert.Equal(80, state.Heat);
		Assert.Contains("cryoplume-a", state.CryoPlumesCooledThisTurn);
	}

	[Fact]
	public void UpkeepInPlumeIsSkippedWhileOverheated()
	{
		var battle = BattleTestFixture.BeginSimulation(Origin);
		var world = battle.Engine.World;
		var state = world.StateOf(PlayerId);
		AddPlume(battle, "cryoplume-a", Origin);
		state.Heat = 100;
		state.Overheat = true;
		
		new TurnUpkeepEffect().Apply(world, battle.Engine.ActorRuntimes.For(PlayerId), PlayerId);

		Assert.Equal(100, state.Heat);
	}

	[Fact]
	public void EncounterPlumeSpawnSurvivesSaveAndBecomesPassableWorldPlume()
	{
		var plumeCell = new Coord(3, 3, 3);
		var encounter = new BattleEncounter
		{
			Seed = 1,
			Id = "plume-encounter",
			Objective = EObjective.EliminateOpponents,
			Spawns =
			[
				Spawn("player-ship", ETeam.Player, EFaction.Player, Coord.Zero, new UserExecutionAgent()),
				Spawn("enemy-ship", ETeam.Enemy, EFaction.Pirates, new Coord(8, 0, 0), BattleAgentFactory.Create(EBattleAgentKind.Ai)),
			],
			WorldHazards =
			[
				new BattleHazardSpawn
				{
					Kind = EBattleHazardKind.CryoPlume,
					Origin = plumeCell,
					Cells = new HashSet<Coord> { plumeCell },
				},
			],
		};

		var restored = SaveDtoMapper.RestoreBattleEncounter(SaveDtoMapper.CaptureBattleEncounter(encounter));
		using var battle = BattleOrchestrator.FromEncounter(restored, gridSize: 12);

		Assert.NotNull(battle.Engine.World.CryoPlumeAt(plumeCell));
		Assert.False(battle.Engine.World.IsCellBlocked(plumeCell));
	}

	private static BattleSpawn Spawn(string shipId, ETeam team, EFaction faction, Coord position, ExecutionAgent<BattleWorld, ActorRuntime> agent) =>
		new()
		{
			Ship = ShipInstance.FromCatalog(shipId, EType.Fighter),
			Team = team,
			Faction = faction,
			Position = position,
			ExecutionAgent = agent,
		};

	private static Coord AddPlume(BattleOrchestrator battle, string id, Coord cell)
	{
		battle.Engine.World.AddNonUnit(CryoPlume.Create(id, cell, battle.Engine.World.Grid));
		return cell;
	}
}
