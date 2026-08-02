using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Ai;

internal static class EnemySearchInput
{
	internal const int MomentumWeight = 1_000;
	internal const int UnusedApPenalty = 100;
	internal const int RailgunHitBonus = 2_000;
	internal const int TimelineRefinementSlack = UnusedApPenalty;

	public static SearchInput<BattleWorld, ActorRuntime> ForTurn(
		IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> capabilities) =>
		new(
			BattleSearchVisit.ForCapabilities,
			(sim, actorId, searchStartDepth, context) =>
				ShouldPrune(sim, actorId, searchStartDepth, context, capabilities));

	public static int ScoreHeuristic(
		SearchFrame<BattleWorld, ActorRuntime> frame,
		BattleSimulation anchor,
		string actorId,
		int searchStartDepth)
	{
		var world = frame.World.Fork();
		var runtimes = frame.Runtimes.Fork();
		ExecutionHelper.Apply(new EndOfPhaseAction(actorId), world, runtimes.For(actorId));

		var state = world.StateOf(actorId);
		if (!state.IsAlive)
			return int.MinValue;

		var score = state.MomentumLevel * MomentumWeight - state.ActionPoints * UnusedApPenalty;
		score += RailgunAdjustment(anchor, frame.Actions, actorId, searchStartDepth);
		return score;
	}

	private static bool ShouldPrune(
		BattleSimulation sim,
		string actorId,
		int searchStartDepth,
		SearchContext context,
		IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> capabilities)
	{
		var upperBound = UpperBound(sim, actorId);
		if (context.BestScore != int.MinValue && upperBound < context.BestScore - TimelineRefinementSlack)
			return true;

		if (!IsTerminal(sim, actorId, capabilities))
			return false;

		var score = ScoreHeuristic(sim, actorId, searchStartDepth);
		if (score > context.BestScore)
			context.BestScore = score;

		return false;
	}

	private static int UpperBound(BattleSimulation sim, string actorId)
	{
		var state = sim.StateOf<ActorState>(actorId);
		var score = state.MomentumLevel * MomentumWeight - state.ActionPoints * UnusedApPenalty;
		if (state.RailgunRemaining > 0 && RailgunWouldHit(sim, actorId, sim.Actions.Count))
			score += RailgunHitBonus;

		return score;
	}

	private static int ScoreHeuristic(BattleSimulation sim, string actorId, int searchStartDepth)
	{
		var world = sim.World.Fork();
		var runtimes = sim.Runtimes.Fork();
		ExecutionHelper.Apply(new EndOfPhaseAction(actorId), world, runtimes.For(actorId));

		var state = world.StateOf(actorId);
		if (!state.IsAlive)
			return int.MinValue;

		var score = state.MomentumLevel * MomentumWeight - state.ActionPoints * UnusedApPenalty;
		score += RailgunAdjustment(sim, sim.Actions, actorId, searchStartDepth);
		return score;
	}

	private static bool IsTerminal(
		BattleSimulation sim,
		string actorId,
		IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> capabilities)
	{
		var state = sim.StateOf<ActorState>(actorId);
		if (state.ActionPoints == 0)
			return true;

		var runtime = sim.RuntimeFor(actorId);
		foreach (var def in capabilities)
		{
			foreach (var candidate in def.Discover(sim.World, runtime, actorId))
			{
				if (def.IsLegal(candidate, sim.World, runtime))
					return false;
			}
		}

		return true;
	}

	private static int RailgunAdjustment(
		BattleSimulation anchor,
		IReadOnlyList<IAction> actions,
		string actorId,
		int searchStartDepth)
	{
		for (var i = searchStartDepth; i < actions.Count; i++)
		{
			if (actions[i] is not RailgunAction { ActorId: var railgunActorId } || railgunActorId != actorId)
				continue;

			return RailgunWouldHit(anchor, actions, actorId, i, searchStartDepth)
				? RailgunHitBonus
				: -RailgunHitBonus;
		}

		return 0;
	}

	private static bool RailgunWouldHit(BattleSimulation sim, string actorId, int actionIndex)
	{
		var world = sim.ReplayWorld(actionIndex);
		return RailgunWouldHit(world, actorId);
	}

	private static bool RailgunWouldHit(
		BattleSimulation anchor,
		IReadOnlyList<IAction> actions,
		string actorId,
		int actionIndex,
		int searchStartDepth)
	{
		var world = WorldAtAction(anchor, actions, searchStartDepth, actionIndex);
		return RailgunWouldHit(world, actorId);
	}

	private static bool RailgunWouldHit(BattleWorld world, string actorId)
	{
		var frame = BodyFrame.From(world.StateOf(actorId));
		var cells = WeaponBursts.RailgunBurstCells(frame, world.Grid.IsInBounds);
		return world.AnyOpponentInCells(actorId, cells);
	}

	private static BattleWorld WorldAtAction(
		BattleSimulation anchor,
		IReadOnlyList<IAction> actions,
		int searchStartDepth,
		int actionIndex)
	{
		var world = anchor.ReplayWorld(searchStartDepth);
		var runtimes = anchor.Runtimes.Fork();
		for (var i = searchStartDepth; i < actionIndex; i++)
			ExecutionHelper.Apply(actions[i], world, runtimes.For(actions[i]));

		return world;
	}
}
