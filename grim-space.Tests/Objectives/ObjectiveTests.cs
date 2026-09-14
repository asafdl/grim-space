using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.World;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Actions;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Objectives;

public sealed class ObjectiveTests
{
	private const string PlayerId = "player";

	[Fact]
	public void TorpedoDeathDoesNotEndBattleWhilePlayerAliveAndEnemyAlive()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		battle.Engine.World.StateOf(torpedoId).HullPoints = 0;
		BattleTestFixture.ResetPlayerPlanning(battle);

		_ = BattleTestActions.CommitAndResolve(battle);

		Assert.Equal(EBattleResult.Ongoing, battle.Outcome.Result);
	}

	[Fact]
	public void EliminateOpponents_ReportsDestroyedOnlyAfterAllParticipantUnitsDie()
	{
		var encounter = new BattleEncounter
		{
			Seed = 1,
			Objective = EObjective.EliminateOpponents,
			Spawns =
			[
				Spawn("player-ship", Alliance.Player, new Coord(2, 2, 2), new UserExecutionAgent()),
				Spawn("pirate-1", Alliance.Enemy, new Coord(8, 2, 2), new AiController()),
				Spawn("pirate-2", Alliance.Enemy, new Coord(8, 3, 2), new AiController()),
			],
			Participants =
			[
				new BattleParticipant("player-fleet", ["player-ship"]),
				new BattleParticipant("pirate-fleet", ["pirate-1", "pirate-2"]),
			],
		};
		using var battle = BattleOrchestrator.FromEncounter(encounter, gridSize: 12);
		var objectives = new Manager(
			encounter.Objective,
			encounter.Participants,
			UnitRegistry.For(battle.Engine.World));

		battle.Engine.World.StateOf("pirate-1").HullPoints = 0;
		var ongoing = objectives.Evaluate(battle.Engine.World, battle.PlayerId);

		Assert.Equal(EBattleResult.Ongoing, ongoing.Result);
		Assert.Equal(EBattleParticipantState.Alive, ongoing.StateOf("player-fleet"));
		Assert.Equal(EBattleParticipantState.Alive, ongoing.StateOf("pirate-fleet"));

		battle.Engine.World.StateOf("pirate-2").HullPoints = 0;
		var victory = objectives.Evaluate(battle.Engine.World, battle.PlayerId);

		Assert.Equal(EBattleResult.Win, victory.Result);
		Assert.Equal(EBattleParticipantState.Alive, victory.StateOf("player-fleet"));
		Assert.Equal(EBattleParticipantState.Destroyed, victory.StateOf("pirate-fleet"));
	}

	[Fact]
	public void Evaluate_ThreePatrolFleet_ReportsDestroyedPatrolOutcomes()
	{
		var encounter = new BattleEncounter
		{
			Seed = 1,
			Objective = EObjective.EliminateOpponents,
			Spawns =
			[
				Spawn("player-ship", Alliance.Player, new Coord(2, 2, 2), new UserExecutionAgent()),
				Spawn("patrol-0", Alliance.Enemy, new Coord(8, 2, 2), new AiController(), EType.Patrol),
				Spawn("patrol-1", Alliance.Enemy, new Coord(8, 3, 2), new AiController(), EType.Patrol),
				Spawn("patrol-2", Alliance.Enemy, new Coord(8, 4, 2), new AiController(), EType.Patrol),
			],
			Participants =
			[
				new BattleParticipant("player-fleet", ["player-ship"]),
				new BattleParticipant("pirate-fleet", ["patrol-0", "patrol-1", "patrol-2"]),
			],
		};
		using var battle = BattleOrchestrator.FromEncounter(encounter, gridSize: 12);
		var objectives = new Manager(
			encounter.Objective,
			encounter.Participants,
			UnitRegistry.For(battle.Engine.World));

		foreach (var patrolId in new[] { "patrol-0", "patrol-1", "patrol-2" })
			battle.Engine.World.StateOf(patrolId).HullPoints = 0;

		var outcome = objectives.Evaluate(battle.Engine.World, battle.PlayerId);

		Assert.Equal(3, outcome.TacticalUnitOutcomes.Count(unit => unit.Kind == EType.Patrol));
		foreach (var patrolId in new[] { "patrol-0", "patrol-1", "patrol-2" })
		{
			var patrolOutcome = outcome.TacticalUnitOutcomes.Single(unit => unit.TacticalUnitId == patrolId);
			Assert.Equal("pirate-fleet", patrolOutcome.OwnerParticipantId);
			Assert.Equal(EType.Patrol, patrolOutcome.Kind);
			Assert.Equal(EBattleParticipantState.Destroyed, patrolOutcome.State);
		}
	}

	[Fact]
	public void Evaluate_SpawnedPatrol_InheritsDefeatedFleetOwnershipViaParentId()
	{
		var battle = BattleTestFixture.BeginCarrierVsPlayer(new Coord(0, 5, 5), new Coord(8, 5, 5));
		var carrierId = BattleTestFixture.FirstEnemyId(battle);
		_ = BattleTestActions.CommitAndResolve(battle);

		var patrol = Assert.Single(
			UnitRegistry.For(battle.Engine.World).All,
			unit => unit.State.Type == EType.Patrol);
		patrol.State.HullPoints = 0;
		battle.Engine.World.StateOf(carrierId).HullPoints = 0;

		var objectives = new Manager(
			EObjective.EliminateOpponents,
			[
				new BattleParticipant("player-fleet", [battle.PlayerId]),
				new BattleParticipant("pirate-fleet", [carrierId]),
			],
			UnitRegistry.For(battle.Engine.World));
		var outcome = objectives.Evaluate(battle.Engine.World, battle.PlayerId);

		var patrolOutcome = outcome.TacticalUnitOutcomes.Single(unit => unit.TacticalUnitId == patrol.State.Id);
		Assert.Equal("pirate-fleet", patrolOutcome.OwnerParticipantId);
		Assert.Equal(EType.Patrol, patrolOutcome.Kind);
		Assert.Equal(EBattleParticipantState.Destroyed, patrolOutcome.State);
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

	private static BattleSpawn Spawn(
		string id,
		Alliance alliance,
		Coord position,
		ExecutionAgent<BattleWorld, ActorRuntime> executionAgent,
		EType type = EType.Fighter) =>
		new()
		{
			Unit = new Instance
			{
				Id = id,
				Type = type,
				Alliance = alliance,
			},
			Position = position,
			ExecutionAgent = executionAgent,
		};
}
