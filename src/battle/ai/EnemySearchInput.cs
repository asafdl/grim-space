using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Dfs;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Ai;

internal static class EnemySearchInput
{
	internal const int MomentumWeight = 1_000;
	internal const int UnusedApPenalty = 100;
	internal const int RailgunHitBonus = 2_000;
	internal const int TimelineRefinementSlack = UnusedApPenalty;
	internal const int FacingWeight = 800;
	internal const int ApproachWeight = 150;

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
		score += EngagementAdjustment(world, actorId, anchor, frame.Actions, searchStartDepth);
		return score;
	}

	public static int UpperBound(BattleWorld world, string actorId)
	{
		var state = world.StateOf(actorId);
		// Optimistic bound: remaining AP may still be spent productively; momentum can climb to max.
		var score = MomentumConfig.MaxLevel * MomentumWeight;
		if (state.RailgunRemaining > 0)
			score += RailgunHitBonus;

		return score;
	}

	private static int EngagementAdjustment(
		BattleWorld world,
		string actorId,
		BattleSimulation anchor,
		IReadOnlyList<IAction> actions,
		int searchStartDepth)
	{
		var state = world.StateOf(actorId);
		if (state.RailgunRemaining <= 0 || RailgunFired(actions, actorId, searchStartDepth))
			return 0;

		if (RailgunWouldHit(world, actorId))
			return 0;

		var opponent = NearestOpponent(world, actorId);
		if (opponent is null)
			return 0;

		var bonus = 0;
		if (IsFacing(state, opponent.State.Position))
			bonus += FacingWeight;

		var startPosition = anchor.ReplayWorld(searchStartDepth).StateOf(actorId).Position;
		var distanceClosed = startPosition.ManhattanDistanceTo(opponent.State.Position)
			- state.Position.ManhattanDistanceTo(opponent.State.Position);
		if (distanceClosed > 0)
			bonus += distanceClosed * ApproachWeight;

		return bonus;
	}

	private static bool RailgunFired(IReadOnlyList<IAction> actions, string actorId, int searchStartDepth)
	{
		for (var i = searchStartDepth; i < actions.Count; i++)
		{
			if (actions[i] is RailgunAction { ActorId: var railgunActorId } && railgunActorId == actorId)
				return true;
		}

		return false;
	}

	private static Unit? NearestOpponent(BattleWorld world, string actorId)
	{
		var units = UnitRegistry.For(world);
		var actor = units.UnitOf(actorId);
		Unit? nearest = null;
		var bestDistance = int.MaxValue;

		foreach (var unit in units.Except(actorId))
		{
			if (!unit.State.IsAlive || actor.RelationTo(unit) != EUnitRelation.Opponent)
				continue;

			var distance = actor.State.Position.ManhattanDistanceTo(unit.State.Position);
			if (distance >= bestDistance)
				continue;

			bestDistance = distance;
			nearest = unit;
		}

		return nearest;
	}

	private static bool IsFacing(State actor, Coord target) =>
		actor.Fore == AxisToward(actor.Position, target);

	private static Coord AxisToward(Coord from, Coord to)
	{
		var delta = to - from;
		var ax = System.Math.Abs(delta.X);
		var ay = System.Math.Abs(delta.Y);
		var az = System.Math.Abs(delta.Z);

		if (ax >= ay && ax >= az)
			return new Coord(System.Math.Sign(delta.X), 0, 0);

		if (ay >= ax && ay >= az)
			return new Coord(0, System.Math.Sign(delta.Y), 0);

		return new Coord(0, 0, System.Math.Sign(delta.Z));
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
