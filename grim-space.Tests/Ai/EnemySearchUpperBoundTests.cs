using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Dfs;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Ai;

[BattleTestSuite]
public sealed class EnemySearchUpperBoundTests
{
	[Theory]
	[InlineData(0, 3, 1)]
	[InlineData(2, 4, 1)]
	[InlineData(0, 2, 2)]
	public void NoDescendantOutscoresAncestorUpperBound(int stepsBeyondReach, int actionPoints, int charges)
	{
		var simulation = (BattleSimulation)BattleTestFixture.LightningCannonDuel(stepsBeyondReach, actionPoints);
		var actor = simulation.World.StateOf("enemy");
		var cannon = actor.FindInstalled(EAbilityKind.LightningCannon)!;
		actor.MountRuntimeFor(cannon.Mount).UsesRemaining = charges;
		// Modify the anchor, not just its disposable preview, so forks retain the second charge.
		var battle = BattleTestFixture.BeginSimulation(
			simulation.World.UnitRegistry.UnitOf("player"), simulation.World.UnitRegistry.UnitOf("enemy"),
			simulation.World.Grid);
		AssertBounds((BattleSimulation)battle.Engine.CreateSimulation());
	}

	[Fact]
	public void GoopRecoilAndRamDoNotOutscoreAncestorBounds()
	{
		var player = Factory.Create(
			ShipInstance.FromCatalog("player", EType.Fighter), ETeam.Player,
			new Coord(3, 5, 5), new AiController(), new Coord(1, 0, 0), Coord.Up);
		var enemy = Factory.Create(
			ShipInstance.FromCatalog("enemy", EType.IndustrialGooper), ETeam.Enemy,
			new Coord(8, 5, 5), new AiController(), new Coord(1, 0, 0), Coord.Up);
		enemy.State.CurrentActionPoints = 1;
		enemy.State.CurrentManeuverPoints = 0;
		using var battle = BattleTestFixture.BeginSimulation(player, enemy);
		AssertBounds((BattleSimulation)battle.Engine.CreateSimulation());
	}

	[Fact]
	public void ZeroApGravityWellAdvanceRemainsInsideBound()
	{
		var center = new Coord(6, 6, 6);
		var grid = BattleTestFixture.Grid(40);
		var well = GravityWell.Create("well", center, BodyFrame.WorldAligned(center), grid);
		var enemy = BattleTestFixture.Enemy(center + new Coord(1, 0, -1));
		enemy.State.CurrentActionPoints = 0;
		enemy.State.CurrentManeuverPoints = 0;
		var player = BattleTestFixture.Player(center + new Coord(1, 0, 20));
		using var battle = BattleTestFixture.BeginSimulation(player, [enemy], grid, hazards: [well]);
		var anchor = (BattleSimulation)battle.Engine.CreateSimulation();
		var branch = anchor.Fork();
		var rootBound = EnemySearchInput.Evaluate(anchor, anchor, "enemy").UpperBound;
		var initialScore = EnemySearchInput.Evaluate(anchor, anchor, "enemy").Score;
		for (var step = 0; step < 3; step++)
		{
			Assert.True(branch.TryEnqueue(new MoveStepAction("enemy")));
			var evaluation = EnemySearchInput.Evaluate(branch, anchor, "enemy");
			Assert.True(evaluation.Score > initialScore);
			Assert.True(evaluation.Score <= rootBound);
		}
		Assert.Equal(0, branch.World.StateOf("enemy").CurrentActionPoints);
	}

	private static void AssertBounds(BattleSimulation simulation)
	{
		var actor = simulation.World.StateOf("enemy");
		var start = simulation.Actions.Count;
		var ancestorBounds = new List<int>();
		var violations = new List<string>();

		foreach (var frame in SearchBridge.Enumerate(
			simulation,
			actor.Id,
			AiController.SearchCapabilities(actor, simulation.RuntimeFor(actor.Id)),
			new SearchInput<BattleWorld, ActorRuntime>(
				BattleSearchVisit.ForCapabilities,
				ShouldExploreAction: EnemySearchInput.ShouldExploreAction),
			TestContext.Current.CancellationToken))
		{
			var depth = frame.Actions.Count - start;
			ancestorBounds.RemoveRange(depth, ancestorBounds.Count - depth);

			var score = EnemySearchInput.ScoreHeuristic(frame, simulation, actor.Id, start);
			if (score != int.MinValue && ancestorBounds.Count > 0 && score > ancestorBounds.Min())
				violations.Add($"score {score} > ancestor bound {ancestorBounds.Min()} via " +
					string.Join(" > ", frame.Actions.Skip(start).Select(action => action.GetType().Name)));

			ancestorBounds.Add(EnemySearchInput.UpperBound(frame, simulation, actor.Id, start));
			var branch = simulation.Fork();
			foreach (var action in frame.Actions.Skip(start))
				Assert.True(branch.TryEnqueue(action));
			var evaluation = EnemySearchInput.Evaluate(branch, simulation, actor.Id);
			Assert.Equal(score, evaluation.Score);
			Assert.Equal(ancestorBounds[^1], evaluation.UpperBound);
		}

		Assert.True(violations.Count == 0, $"{violations.Count} violations, e.g. {string.Join("; ", violations.Take(3))}");
	}
}
