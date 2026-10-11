using GrimSpace.Battle;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Movement;
using GrimSpace.Core.Actions;
using GrimSpace.Tutorials;

namespace GrimSpace.Tests.Tutorials;

public sealed class BattleTutorialMovementMatchTests
{
	[Fact]
	public void MatchesQueuedMovement_AcceptsPoseAfterHeadingAndRoll()
	{
		var battle = BattleOrchestrator.FromEncounter(BattleEncounter.DevDefault(0, gridSize: 64), gridSize: 64);
		var objective = Assert.IsType<BattleTutorialObjectiveResult.Resolved>(
				BattleTutorialObjectives.ResolveTurn2(battle))
			.Objective;
		var session = MovePathEndpoints.DiscoverExtensions(battle.PlayerAgent.Sim, battle.PlayerId)
			.First(candidate =>
				candidate.EndPosition == objective.Destination
				&& candidate.EndBasis == objective.RequiredBasis
				&& candidate.Steps.Count > 0);

		Assert.True(battle.PlayerAgent.TryEnqueue(session.Steps.Cast<IAction>().ToList()));
		Assert.True(BattleTutorialObjectives.MatchesQueuedMovement(
			battle.PlayerAgent.Sim,
			battle.PlayerId,
			battle.PlayerAgent.Sim.Actions,
			objective));
	}
}
