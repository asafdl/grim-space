using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Dfs;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Dfs;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class ActionSearchCorrectnessTests
{
	private const string PlayerId = "player";

	private static readonly IActionDef<IAction, Battle.World.BattleWorld, ActorRuntime, IEffect<Battle.World.BattleWorld, ActorRuntime>>[] MovementActionDefs =
	[
		MoveDef.Instance,
		HeadingDef.Instance,
		RollDef.Instance,
	];

	[Fact]
	public void PrunedFramesAreSubsetOfExhaustiveSearch_MoveOnly()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var pruned = PrefixKeys(battle.PlayerAgent.Sim, [MoveDef.Instance]);
		var exhaustive = PrefixKeysExhaustive(battle.PlayerAgent.Sim, [MoveDef.Instance]);

		Assert.Subset(exhaustive, pruned);
	}

	[Fact]
	public void EveryPrunedFrameReplayableFromTurnStart()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));

		foreach (var frame in SearchBridge.Enumerate(battle.PlayerAgent.Sim, PlayerId, MovementActionDefs,
			new SearchInput<BattleWorld, ActorRuntime>(BattleSearchVisit.ForCapabilities), TestContext.Current.CancellationToken))
			Assert.True(ReplayPrefix(battle, frame.Actions), $"Failed to replay: {string.Join(", ", frame.Actions)}");
	}

	[Fact]
	public void ExhaustiveSearchFindsAtLeastAsManyPrefixesAsPrunedSearch()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var prunedCount = SearchBridge.Enumerate(battle.PlayerAgent.Sim, PlayerId, MovementActionDefs,
			new SearchInput<BattleWorld, ActorRuntime>(BattleSearchVisit.ForCapabilities), TestContext.Current.CancellationToken).Count();
		var exhaustiveCount = ActionSearchExhaustive.Run(battle.PlayerAgent.Sim, PlayerId, MovementActionDefs).Count();

		Assert.True(exhaustiveCount >= prunedCount);
	}

	[Fact]
	public void PrunedAndExhaustiveAgreeOnRootFrame()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var prunedRoot = SearchBridge.Enumerate(battle.PlayerAgent.Sim, PlayerId, MovementActionDefs,
			new SearchInput<BattleWorld, ActorRuntime>(BattleSearchVisit.ForCapabilities), TestContext.Current.CancellationToken).First();
		var exhaustiveRoot = ActionSearchExhaustive.Run(battle.PlayerAgent.Sim, PlayerId, MovementActionDefs).First();

		Assert.Equal(prunedRoot.Actions, exhaustiveRoot.Actions);
		Assert.Equal(0, prunedRoot.Depth);
		Assert.Equal(0, exhaustiveRoot.Depth);
	}

	[Theory]
	[InlineData(false, 0)]
	[InlineData(true, 0)]
	[InlineData(false, 1_000)]
	[InlineData(true, 1_000)]
	public void CapabilityPruningPreservesDamagingPlanRegardlessOfDiscoveryOrder(bool attackFirst, int shields)
	{
		const string actorId = "enemy";
		var source = BattleTestFixture.LightningCannonDuel(stepsBeyondReach: 1, actionPoints: 1, targetShieldPoints: shields);
		IAction[] expected = [new MoveStepAction(actorId), new LightningCannonAction(actorId)];
		var hit = source.Fork();
		foreach (var action in expected)
			Assert.True(hit.TryEnqueue(action));
		var miss = source.Fork();
		foreach (var action in expected.Reverse())
			Assert.True(miss.TryEnqueue(action));
		var hitVisit = BattleSearchVisit.ForCapabilities(hit, actorId);
		var missVisit = BattleSearchVisit.ForCapabilities(miss, actorId);
		Assert.NotEqual(hitVisit.State, missVisit.State);
		Assert.Equal(hitVisit.Budget, missVisit.Budget);
		Assert.Equal(1_000, miss.World.StateOf(PlayerId).CurrentHullPoints);
		if (shields == 0)
			Assert.True(hit.World.StateOf(PlayerId).CurrentHullPoints < 1_000);
		else
		{
			Assert.Equal(1_000, hit.World.StateOf(PlayerId).CurrentHullPoints);
			Assert.False(hit.World.StateOf(PlayerId).CurrentShieldPoints
				.Matches(miss.World.StateOf(PlayerId).CurrentShieldPoints));
		}

		IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>[] definitions =
			attackFirst ? [LightningCannonDef.Instance, MoveDef.Instance] : [MoveDef.Instance, LightningCannonDef.Instance];
		var frames = SearchBridge.Enumerate(source, actorId, definitions,
			new SearchInput<BattleWorld, ActorRuntime>(BattleSearchVisit.ForCapabilities), TestContext.Current.CancellationToken).ToArray();
		Assert.Contains(frames, frame => frame.Actions.SequenceEqual(expected));
		var best = frames.Select(frame => (Frame: frame,
				Score: EnemySearchInput.ScoreHeuristic(frame, source, actorId, source.Actions.Count)))
			.OrderByDescending(candidate => candidate.Score).First();
		Assert.Equal(EnemySearchInput.DamageHitBonus, best.Score);
		Assert.Equal(expected, best.Frame.Actions.ToArray());
	}

	[Fact]
	public void CapabilityPruningKeepsHotterStateThatReachesOverdrive()
	{
		const string actorId = "enemy";
		var source = BattleTestFixture.LightningCannonDuel(stepsBeyondReach: 0, actionPoints: 2, heatBelowCapacity: 35);
		var fire = new LightningCannonAction(actorId);
		IAction[] expected =
		[
			new MoveStepAction(actorId, ESpatialOrientation.Port),
			new MoveStepAction(actorId, ESpatialOrientation.Starboard),
			fire,
		];
		var hot = source.Fork();
		foreach (var action in expected)
			Assert.True(hot.TryEnqueue(action));
		var cool = source.Fork();
		Assert.True(cool.TryEnqueue(fire));
		var sidestepped = source.Fork();
		Assert.True(sidestepped.TryEnqueue(new MoveStepAction(actorId, ESpatialOrientation.Port), fire));
		Assert.True(hot.World.StateOf(actorId).Overdrive);
		Assert.False(cool.World.StateOf(actorId).Overdrive);
		Assert.False(sidestepped.World.StateOf(actorId).Overdrive);
		var overdriveHull = hot.World.StateOf(PlayerId).CurrentHullPoints;
		Assert.True(overdriveHull < cool.World.StateOf(PlayerId).CurrentHullPoints);

		var origin = source.World.StateOf(actorId).Position;
		IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>[] definitions =
			[MoveDef.Instance, LightningCannonDef.Instance];
		var frames = SearchBridge.Enumerate(source, actorId, definitions,
			new SearchInput<BattleWorld, ActorRuntime>(BattleSearchVisit.ForCapabilities), TestContext.Current.CancellationToken).ToArray();
		Assert.Contains(frames, frame => frame.World.StateOf(actorId).Position == origin
			&& frame.World.StateOf(PlayerId).CurrentHullPoints == overdriveHull);
	}

	private static HashSet<string> PrefixKeys(
		BattleSimulation sim,
		IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> actionDefs) =>
		SearchBridge.Enumerate(sim, PlayerId, actionDefs,
			new SearchInput<BattleWorld, ActorRuntime>(BattleSearchVisit.ForCapabilities), TestContext.Current.CancellationToken)
			.Select(frame => PrefixKey(frame.Actions))
			.ToHashSet();

	private static HashSet<string> PrefixKeysExhaustive(
		BattleSimulation sim,
		IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> actionDefs) =>
		ActionSearchExhaustive.Run(sim, PlayerId, actionDefs)
			.Select(frame => PrefixKey(frame.Actions))
			.ToHashSet();

	private static string PrefixKey(IReadOnlyList<IAction> actions) =>
		actions.Count == 0
			? string.Empty
			: string.Join('|', actions.Select(ActionKey));

	private static string ActionKey(IAction action) =>
		action switch
		{
			MoveStepAction move => $"move:{move.ActorId}",
			HeadingTurnAction heading => $"heading:{heading.ActorId}:{heading.Turn}",
			RollAction roll => $"roll:{roll.ActorId}:{roll.Direction}",
			ScrapDroneSwarmAction swarm => $"swarm:{swarm.ActorId}:{swarm.MountedOn}",
			LightningCannonAction lightningCannon => $"lightningCannon:{lightningCannon.ActorId}",
			_ => action.GetType().FullName ?? "action",
		};

	private static bool ReplayPrefix(BattleOrchestrator battle, IReadOnlyList<IAction> prefix)
	{
		var sim = battle.Engine.CreateSimulation();
		foreach (var action in prefix)
		{
			if (!sim.TryEnqueue(action))
				return false;
		}

		return true;
	}
}
