using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Abilities;
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
	internal const int DamageHitBonus = 2_000;
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
		score += DamageAdjustment(anchor, frame.Actions, actorId, searchStartDepth);
		score += EngagementAdjustment(world, actorId, anchor, frame.Actions, searchStartDepth);
		return score;
	}

	public static int UpperBound(BattleWorld world, string actorId)
	{
		var state = world.StateOf(actorId);
		// Optimistic bound: remaining AP may still be spent productively; momentum can climb to max.
		var score = MomentumConfig.MaxLevel * MomentumWeight;
		if (!HasOffensiveCharges(state))
			return score;

		var opponent = NearestOpponent(world, actorId);
		var weaponReach = OptimisticWeaponReach(state);
		if (weaponReach > 0
			&& opponent is not null
			&& OffensiveReach.CouldPossiblyDamage(
				state.Position,
				state.ActionPoints,
				opponent.State.Position,
				weaponReach))
			score += DamageHitBonus;

		return score;
	}

	public static bool HasDamageHit(
		BattleSimulation anchor,
		IReadOnlyList<IAction> actions,
		string actorId,
		int searchStartDepth) =>
		DamageAdjustment(anchor, actions, actorId, searchStartDepth) == DamageHitBonus;

	private static int EngagementAdjustment(
		BattleWorld world,
		string actorId,
		BattleSimulation anchor,
		IReadOnlyList<IAction> actions,
		int searchStartDepth)
	{
		var state = world.StateOf(actorId);
		if (!HasOffensiveCharges(state) || OffensiveFired(actions, actorId, searchStartDepth))
			return 0;

		if (CanDamageNow(world, actorId))
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

	private static bool HasOffensiveCharges(State state) =>
		state.RailgunRemaining > 0 || state.FlakRemaining > 0;

	private static int OptimisticWeaponReach(State state)
	{
		var reach = 0;
		if (state.RailgunRemaining > 0)
			reach = System.Math.Max(reach, CombatConfig.MaxRailgunManhattanRange);
		if (state.FlakRemaining > 0)
			reach = System.Math.Max(reach, CombatConfig.MaxFlakManhattanRange);
		return reach;
	}

	private static bool OffensiveFired(IReadOnlyList<IAction> actions, string actorId, int searchStartDepth)
	{
		for (var i = searchStartDepth; i < actions.Count; i++)
		{
			if (actions[i] is RailgunAction { ActorId: var railgunActorId } && railgunActorId == actorId)
				return true;

			if (actions[i] is FlakAction { ActorId: var flakActorId } && flakActorId == actorId)
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

	private static int DamageAdjustment(
		BattleSimulation anchor,
		IReadOnlyList<IAction> actions,
		string actorId,
		int searchStartDepth)
	{
		for (var i = searchStartDepth; i < actions.Count; i++)
		{
			var action = actions[i];
			if (action is not RailgunAction and not FlakAction)
				continue;

			if (action.ActorId != actorId)
				continue;

			return WouldDamage(anchor, actions, actorId, i, searchStartDepth)
				? DamageHitBonus
				: -DamageHitBonus;
		}

		return 0;
	}

	private static bool WouldDamage(
		BattleSimulation anchor,
		IReadOnlyList<IAction> actions,
		string actorId,
		int actionIndex,
		int searchStartDepth)
	{
		var world = WorldAtAction(anchor, actions, searchStartDepth, actionIndex);
		return WouldDamage(world, actorId, actions[actionIndex]);
	}

	private static bool WouldDamage(BattleWorld world, string actorId, IAction action) =>
		action switch
		{
			RailgunAction { ActorId: var railgunActorId } when railgunActorId == actorId =>
				WouldRailgunDamage(world, actorId),
			FlakAction { ActorId: var flakActorId, MountedOn: var mountedOn } when flakActorId == actorId =>
				WouldFlakDamage(world, actorId, mountedOn),
			_ => false,
		};

	private static bool CanDamageNow(BattleWorld world, string actorId)
	{
		var state = world.StateOf(actorId);
		if (state.RailgunRemaining > 0 && WouldRailgunDamage(world, actorId))
			return true;

		if (state.FlakRemaining <= 0)
			return false;

		return WouldFlakDamage(world, actorId, ESpatialOrientation.Port)
			|| WouldFlakDamage(world, actorId, ESpatialOrientation.Starboard);
	}

	private static bool WouldRailgunDamage(BattleWorld world, string actorId)
	{
		var frame = BodyFrame.From(world.StateOf(actorId));
		var cells = WeaponBursts.RailgunBurstCells(frame, world.Grid.IsInBounds);
		return world.AnyOpponentInCells(actorId, cells);
	}

	private static bool WouldFlakDamage(BattleWorld world, string actorId, ESpatialOrientation mountedOn)
	{
		var frame = BodyFrame.From(world.StateOf(actorId));
		var cells = WeaponBursts.FlakBurstCells(frame, mountedOn, world.Grid.IsInBounds);
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
