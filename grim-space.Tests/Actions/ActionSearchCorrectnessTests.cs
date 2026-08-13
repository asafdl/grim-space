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
	public void PrunedFramesAreSubsetOfExhaustiveSearch_MovementCapabilities()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var pruned = PrefixKeys(battle.PlayerAgent.Sim, MovementActionDefs);
		var exhaustive = PrefixKeysExhaustive(battle.PlayerAgent.Sim, MovementActionDefs);

		Assert.Subset(exhaustive, pruned);
	}

	[Fact]
	public void EveryPrunedFrameReplayableFromTurnStart()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));

		foreach (var frame in ActionSearch.Run(battle.PlayerAgent.Sim, PlayerId, MovementActionDefs, BattleSearchVisit.ForCapabilities))
			Assert.True(ReplayPrefix(battle, frame.Actions), $"Failed to replay: {string.Join(", ", frame.Actions)}");
	}

	[Fact]
	public void EveryExhaustiveFrameReplayableFromTurnStart()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));

		foreach (var frame in ActionSearchExhaustive.Run(battle.PlayerAgent.Sim, PlayerId, MovementActionDefs))
			Assert.True(ReplayPrefix(battle, frame.Actions), $"Failed to replay: {string.Join(", ", frame.Actions)}");
	}

	[Fact]
	public void ExhaustiveSearchFindsAtLeastAsManyPrefixesAsPrunedSearch()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var prunedCount = ActionSearch.Run(battle.PlayerAgent.Sim, PlayerId, MovementActionDefs, BattleSearchVisit.ForCapabilities).Count();
		var exhaustiveCount = ActionSearchExhaustive.Run(battle.PlayerAgent.Sim, PlayerId, MovementActionDefs).Count();

		Assert.True(exhaustiveCount >= prunedCount);
	}

	[Fact]
	public void PrunedAndExhaustiveAgreeOnRootFrame()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var prunedRoot = ActionSearch.Run(battle.PlayerAgent.Sim, PlayerId, MovementActionDefs, BattleSearchVisit.ForCapabilities).First();
		var exhaustiveRoot = ActionSearchExhaustive.Run(battle.PlayerAgent.Sim, PlayerId, MovementActionDefs).First();

		Assert.Equal(prunedRoot.Actions, exhaustiveRoot.Actions);
		Assert.Equal(0, prunedRoot.Depth);
		Assert.Equal(0, exhaustiveRoot.Depth);
	}

	private static HashSet<string> PrefixKeys(
		BattleSimulation sim,
		IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> actionDefs) =>
		ActionSearch.Run(sim, PlayerId, actionDefs, BattleSearchVisit.ForCapabilities)
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
			MoveStepAction move => $"move:{move.ActorId}:{move.Direction}",
			HeadingTurnAction heading => $"heading:{heading.ActorId}:{heading.Turn}",
			RollAction roll => $"roll:{roll.ActorId}:{roll.Direction}",
			FlakAction flak => $"flak:{flak.ActorId}:{flak.MountedOn}",
			RailgunAction railgun => $"railgun:{railgun.ActorId}",
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
