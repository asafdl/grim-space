using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Presentation;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Actions;

namespace GrimSpace.Tests.Movement;

public sealed class MoveEndpointOracleTests
{
	[Fact]
	public void IndexRootEndpointsMatchIndependentMoveOnlySearch()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var index = MoveOptionIndex.FromSimulation(battle.Sim, battle.PlayerId);

		var indexed = index.GetPaths([]).Select(path => path.EndPosition).ToHashSet();
		var oracle = MoveEndpointOracle.DiscoverEndpointPositions(battle, []);

		Assert.Equal(oracle, indexed);
	}

	[Fact]
	public void IndexEndpointsMatchOracleForEveryCachedPrefix()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var index = MoveOptionIndex.FromSimulation(battle.Sim, battle.PlayerId);

		foreach (var prefix in index.EnumeratePrefixes())
		{
			if (prefix.Any(action => action is FlakAction or RailgunAction))
				continue;

			var indexed = index.GetPaths(prefix).Select(path => path.EndPosition).ToHashSet();
			var oracle = MoveEndpointOracle.DiscoverEndpointPositions(battle, prefix);

			Assert.Equal(
				oracle,
				indexed);
		}
	}

	[Fact]
	public void MoveUiDoesNotMatchOracleWhenPrefixIsMutatedAfterIndexBuild()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var ui = BattleTestFixture.Ui(battle);
		_ = ui.MoveUi;

		Assert.True(battle.Sim.TryEnqueue(new HeadingTurnAction(battle.PlayerId, EHeadingTurn.YawRight)));

		var indexed = ui.MoveUi.GetMovePaths(battle.Sim.Actions).Select(path => path.EndPosition).ToHashSet();
		var oracle = MoveEndpointOracle.DiscoverEndpointPositions(battle, battle.Sim.Actions);

		Assert.Equal(oracle, indexed);
	}
}
