using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Player;
using GrimSpace.Math.Grid;
using GrimSpace.Tutorials;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.Factions;

namespace GrimSpace.Tests.Tutorials;

[BattleTestSuite]
public sealed class BattleTutorialMovementMatchTests
{
	[Fact]
	public void MatchesQueuedMovement_AcceptsPoseAfterHeadingAndRoll()
	{
		using var battle = CreateBattle();
		var session = MovePathEndpoints.DiscoverExtensions(battle.PlayerAgent.Sim, battle.PlayerId)
			.First(candidate => candidate.Steps.Any(step => step is HeadingTurnAction)
				&& candidate.Steps.Any(step => step is RollAction));
		var objective = new BattleTutorialObjective("target", session.EndPosition, session.EndBasis);

		Assert.True(battle.PlayerAgent.TryEnqueue(session.Steps));
		Assert.True(BattleTutorialObjectives.MatchesQueuedMovement(
			battle.PlayerAgent.Sim,
			battle.PlayerId,
			battle.PlayerAgent.Sim.Actions,
			objective));
	}

	[Fact]
	public void HasQueuedShieldShot_RequiresCannonAndClearsOnUndo()
	{
		using var battle = CreateBattle();
		var objective = ResolveObjective(battle);
		var agent = battle.PlayerAgent;
		var path = MovePathEndpoints.DiscoverExtensions(agent.Sim, battle.PlayerId)
			.First(candidate => candidate.EndPosition == objective.Destination
				&& candidate.EndBasis == objective.RequiredBasis);

		Assert.True(agent.TryEnqueue(path.Steps));
		Assert.False(BattleTutorialObjectives.HasQueuedShieldShot(agent.Sim, battle.PlayerId, objective));
		Assert.True(agent.TryEnqueue([new LightningCannonAction(battle.PlayerId)]));
		Assert.True(BattleTutorialObjectives.HasQueuedShieldShot(agent.Sim, battle.PlayerId, objective));
		Assert.False(BattleTutorialObjectives.HasQueuedKill(agent.Sim, battle.PlayerId, objective.TargetId));
		Assert.True(agent.Undo());
		Assert.False(BattleTutorialObjectives.HasQueuedShieldShot(agent.Sim, battle.PlayerId, objective));
	}

	[Fact]
	public void HasQueuedKill_RequiresLethalImpactOnTheTutorialTargetAndClearsOnUndo()
	{
		using var battle = CreateBattle();
		var objective = ResolveObjective(battle);
		var agent = battle.PlayerAgent;
		var path = MovePathEndpoints.DiscoverExtensions(agent.Sim, battle.PlayerId)
			.First(candidate => candidate.EndPosition == new Coord(12, 9, 12)
				&& candidate.EndBasis.Forward == new Coord(0, 1, 0));

		Assert.True(agent.TryEnqueue(path.Steps));
		Assert.False(BattleTutorialObjectives.HasQueuedKill(agent.Sim, battle.PlayerId, objective.TargetId));
		Assert.True(agent.TryEnqueue([new LightningCannonAction(battle.PlayerId)]));
		Assert.True(BattleTutorialObjectives.HasQueuedKill(agent.Sim, battle.PlayerId, objective.TargetId));
		Assert.False(BattleTutorialObjectives.HasQueuedKill(agent.Sim, battle.PlayerId, "backup"));
		Assert.False(BattleTutorialObjectives.HasQueuedShieldShot(agent.Sim, battle.PlayerId, objective));
		Assert.True(agent.Undo());
		Assert.False(BattleTutorialObjectives.HasQueuedKill(agent.Sim, battle.PlayerId, objective.TargetId));
	}

	[Fact]
	public void ResolveShieldApproach_RestoresFromAnchorWithAnExistingPlan()
	{
		using var battle = CreateBattle();
		var objective = ResolveObjective(battle);
		var path = MovePathEndpoints.DiscoverExtensions(battle.PlayerAgent.Sim, battle.PlayerId)
			.First(candidate => candidate.EndPosition == objective.Destination
				&& candidate.EndBasis == objective.RequiredBasis);

		Assert.Equal("target", objective.TargetId);
		Assert.Equal(new Coord(9, 12, 12), objective.Destination);
		Assert.True(battle.PlayerAgent.TryEnqueue(path.Steps));
		Assert.Equal(objective, ResolveObjective(battle));
	}

	private static BattleTutorialObjective ResolveObjective(BattleOrchestrator battle) =>
		Assert.IsType<BattleTutorialObjectiveResult.Resolved>(
			BattleTutorialObjectives.ResolveShieldApproach(battle)).Objective;

	private static BattleOrchestrator CreateBattle() =>
		BattleOrchestrator.FromEncounter(new BattleEncounter
		{
			Id = "tutorial-test",
			Seed = 7,
			Objective = EObjective.EliminateOpponents,
			Spawns =
			[
				new BattleSpawn
				{
					Ship = ShipInstance.FromCatalog("fighter", EType.Fighter),
					Team = ETeam.Player,
					Faction = EFaction.Player,
					Position = new Coord(9, 9, 12),
					Fore = new Coord(1, 0, 0),
					ExecutionAgent = new UserExecutionAgent(),
				},
				new BattleSpawn
				{
					Ship = ShipInstance.FromCatalog("target", EType.RepurposedMiner),
					Team = ETeam.Enemy,
					Faction = GrimSpace.Battle.Units.Factory.DefaultFactionFor(ETeam.Enemy),
					Position = new Coord(12, 12, 12),
					Fore = new Coord(-1, 0, 0),
					ExecutionAgent = new AiController(),
				},
				new BattleSpawn
				{
					Ship = ShipInstance.FromCatalog("backup", EType.RepurposedMiner),
					Team = ETeam.Enemy,
					Faction = GrimSpace.Battle.Units.Factory.DefaultFactionFor(ETeam.Enemy),
					Position = new Coord(16, 13, 17),
					Fore = new Coord(-1, 0, 0),
					ExecutionAgent = new AiController(),
				},
			],
		});
}
