using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Dfs;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Dfs;
using BattleDef = GrimSpace.Core.Actions.IActionDef<GrimSpace.Core.Actions.IAction, GrimSpace.Battle.World.BattleWorld, GrimSpace.Battle.Runtime.ActorRuntime, GrimSpace.Core.Actions.IEffect<GrimSpace.Battle.World.BattleWorld, GrimSpace.Battle.Runtime.ActorRuntime>>;

namespace GrimSpace.Tests.Ai;

[BattleTestSuite]
public sealed class BattleSearchVisitTests
{
	[Fact]
	public void EquivalentSearchLocalBombsHaveTheSameVisitKeyWithoutRewritingActions()
	{
		var anchor = CreateSource();
		var visit = BattleSearchVisit.ForCapabilitySearch(anchor);
		var first = Launch(anchor, "first");
		var second = Launch(anchor, "second");

		Assert.Equal(visit(first, "player").State, visit(second, "player").State);
		Assert.Equal(visit(first, "player").Budget, visit(second, "player").Budget);
		Assert.NotEqual(BattleSearchVisit.ForCapabilities(first, "player").State,
			BattleSearchVisit.ForCapabilities(second, "player").State);
		Assert.Equal("first", Assert.IsType<VoidBombAction>(Assert.Single(first.Actions)).SpawnedUnitId);
		Assert.Equal("second", Assert.IsType<VoidBombAction>(Assert.Single(second.Actions)).SpawnedUnitId);
	}

	[Theory]
	[InlineData("position")]
	[InlineData("facing")]
	[InlineData("projectile")]
	[InlineData("status")]
	[InlineData("temporary-shield")]
	[InlineData("cooldown")]
	[InlineData("heat-vents")]
	[InlineData("cover")]
	public void DifferentGameplayStatesAreNotMerged(string difference)
	{
		var anchor = CreateSource();
		var visit = BattleSearchVisit.ForCapabilitySearch(anchor);
		var first = Launch(anchor, "first");
		var second = Launch(anchor, "second");
		var bomb = second.World.StateOf("second");
		var actor = second.World.StateOf("player");
		switch (difference)
		{
			case "position":
				bomb.Position += Coord.Forward;
				break;
			case "facing":
				bomb.Fore = Coord.Forward;
				bomb.Dorsal = Coord.Up;
				bomb.Starboard = Coord.Cross(bomb.Dorsal, bomb.Fore);
				break;
			case "projectile":
				bomb.Projectile = bomb.RequireProjectile() with { BlastRadius = 9 };
				break;
			case "status":
				bomb.Statuses.Add(new SpawnedAllyDamageBuff());
				break;
			case "temporary-shield":
				TemporaryShields.GrantPerFace(actor, "extra", 10);
				break;
			case "cooldown":
				actor.OverchargeEnginesCooldownRemaining = 1;
				break;
			case "heat-vents":
				actor.HeatVentsUsedThisTurn = true;
				break;
			case "cover":
				second.World.AddNonUnit(AsteroidBelt.Create("cover", actor.Position, second.World.Grid, 40));
				break;
		}

		Assert.NotEqual(visit(first, "player").State, visit(second, "player").State);
	}

	[Fact]
	public void EquivalentOverdriveClosuresAndTemporaryGrantIdsDoNotSplitTheState()
	{
		var anchor = CreateSource();
		var visit = BattleSearchVisit.ForCapabilitySearch(anchor);
		var first = Launch(anchor, "first");
		var second = Launch(anchor, "second");
		HeatChangeEffect.FillToCapacity().Apply(first.World, first.RuntimeFor("player"), "player");
		HeatChangeEffect.FillToCapacity().Apply(second.World, second.RuntimeFor("player"), "player");
		TemporaryShields.GrantPerFace(first.World.StateOf("player"), "first-grant", 10);
		TemporaryShields.GrantPerFace(second.World.StateOf("player"), "second-grant", 10);

		Assert.Equal(visit(first, "player").State, visit(second, "player").State);
		var firstStatuses = first.World.StateOf("player").Statuses;
		var secondStatuses = second.World.StateOf("player").Statuses;
		firstStatuses.AddRange(firstStatuses.ToArray().Select(s => s.DeepCopy()));
		secondStatuses.AddRange(secondStatuses.ToArray().Select(s => s.DeepCopy()));
		Assert.Equal(visit(first, "player").State, visit(second, "player").State);
	}

	[Fact]
	public void DistinctTypedStatusesRemainDistinct()
	{
		var anchor = CreateSource();
		var visit = BattleSearchVisit.ForCapabilitySearch(anchor);
		var first = Launch(anchor, "first");
		var second = Launch(anchor, "second");
		first.World.StateOf("player").Statuses.Add(new ReinforcedHull());
		second.World.StateOf("player").Statuses.Add(new YouLittleRat());

		Assert.NotEqual(visit(first, "player").State, visit(second, "player").State);
	}

	[Fact]
	public void BombIdentitiesAlreadyInTheAnchorAreNotCanonicalized()
	{
		var anchor = CreateSource();
		var first = Launch(anchor, "first");
		var second = Launch(anchor, "second");

		Assert.NotEqual(
			BattleSearchVisit.ForCapabilitySearch(first)(first, "player").State,
			BattleSearchVisit.ForCapabilitySearch(second)(second, "player").State);
	}

	[Fact]
	public void CanonicalSearchPreservesTheBestExhaustiveScoreAndReducesFrames()
	{
		var anchor = CreateSource();
		BattleDef[] definitions = [MoveDef.Instance, VoidBombDef.Instance];
		var exhaustive = ActionSearchExhaustive.Run(anchor, "player", definitions, maxDepth: 5).ToArray();
		var search = SearchBridge.Run(anchor, "player", definitions,
			new(BattleSearchVisit.ForCapabilitySearch(anchor)));
		var expected = exhaustive.Max(frame => EnemySearchInput.ScoreHeuristic(frame, anchor, "player", 0));
		var actual = Enumerable.Range(0, search.Count)
			.Max(index => EnemySearchInput.ScoreHeuristic(search.FrameAt(index), anchor, "player", 0));

		Assert.Equal(expected, actual);
		Assert.True(search.Count < exhaustive.Length);
	}

	private static BattleSimulation CreateSource()
	{
		using var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(new Coord(5, 5, 5)),
			BattleTestFixture.Enemy(new Coord(5, 2, 5)));
		battle.Engine.World.StateOf("player").CurrentActionPoints = 3;
		return (BattleSimulation)battle.Engine.CreateSimulation();
	}

	private static BattleSimulation Launch(BattleSimulation anchor, string spawnedId)
	{
		var simulation = anchor.Fork();
		Assert.True(simulation.TryEnqueue(new VoidBombAction("player", ESpatialOrientation.Ventral, spawnedId)));
		return simulation;
	}
}
