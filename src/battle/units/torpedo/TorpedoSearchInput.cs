using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.World;
using GrimSpace.Core.Dfs;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Ai;

internal readonly record struct TorpedoFrameRank(
	bool OpponentInBlast,
	int ApproachGain,
	int MoveCount,
	int Score) : IComparable<TorpedoFrameRank>
{
	public int CompareTo(TorpedoFrameRank other)
	{
		var detonate = OpponentInBlast.CompareTo(other.OpponentInBlast);
		if (detonate != 0)
			return detonate;

		if (OpponentInBlast)
		{
			var moves = other.MoveCount.CompareTo(MoveCount);
			if (moves != 0)
				return moves;
		}
		else
		{
			var approach = ApproachGain.CompareTo(other.ApproachGain);
			if (approach != 0)
				return approach;
		}

		return Score.CompareTo(other.Score);
	}
}

internal static class TorpedoSearchInput
{
	internal const int ApproachWeight = 200;
	internal const int ForwardWeight = 120;
	internal const int BlastTargetBonus = 5_000;
	internal const int BlastAllyPenalty = 2_500;
	internal const int WetBoomPenalty = 4_000;
	internal const int UnusedApPenalty = 50;

	public static Unit? BestReachableOpponent(BattleSimulation session, string actorId)
	{
		var envelope = TorpedoReachEnvelope.Build(session, actorId);
		var units = UnitRegistry.For(session.World);
		var actor = units.UnitOf(actorId);
		Unit? best = null;
		var bestClass = ETorpedoTargetClass.Unreachable;
		var bestTurn = int.MaxValue;
		var bestDistance = int.MaxValue;

		foreach (var unit in units.Except(actorId))
		{
			if (!unit.State.IsAlive
				|| actor.RelationTo(unit) != EUnitRelation.Opponent
				|| unit.State.Type == EType.Torpedo)
			{
				continue;
			}

			var offset = unit.State.Position - actor.State.Position;
			if (Coord.Dot(offset, actor.State.Fore) < 0)
				continue;

			var targetClass = envelope.Classify(unit.State.Position);
			if (targetClass == ETorpedoTargetClass.Unreachable)
				continue;

			var turn = envelope.EarliestReachTurn(unit.State.Position);
			var distance = actor.State.Position.ManhattanDistanceTo(unit.State.Position);

			if (targetClass < bestClass)
				continue;
			if (targetClass == bestClass)
			{
				if (turn > bestTurn)
					continue;
				if (turn == bestTurn && distance >= bestDistance)
					continue;
			}

			best = unit;
			bestClass = targetClass;
			bestTurn = turn;
			bestDistance = distance;
		}

		return best;
	}

	public static TorpedoFrameRank RankFrame(
		SearchFrame<BattleWorld, ActorRuntime> frame,
		BattleSimulation anchor,
		string actorId,
		Unit? target,
		int searchStartDepth)
	{
		var score = ScoreHeuristic(frame, anchor, actorId, target, searchStartDepth);
		if (score == int.MinValue)
			return new(false, 0, 0, score);

		var state = frame.World.StateOf(actorId);
		var start = anchor.ReplayWorld(searchStartDepth).StateOf(actorId);
		var approachGain = ApproachGainToward(start.Position, state.Position, target);
		var opponentInBlast = target is not null
			? state.Position.ManhattanDistanceTo(target.State.Position) <= TorpedoConfig.BlastRadius
			: DetonateDef.HasOpponentInBlast(frame.World, actorId, state.Position);

		return new(opponentInBlast, approachGain, frame.Depth, score);
	}

	public static int ScoreHeuristic(
		SearchFrame<BattleWorld, ActorRuntime> frame,
		BattleSimulation anchor,
		string actorId,
		Unit? target,
		int searchStartDepth)
	{
		var world = frame.World;
		var units = UnitRegistry.For(world);
		if (!units.TryGet(actorId, out var unit) || !unit.State.IsAlive)
			return int.MinValue;

		var state = unit.State;
		var start = anchor.ReplayWorld(searchStartDepth).StateOf(actorId);
		var score = -state.ActionPoints * UnusedApPenalty;

		var displacement = state.Position - start.Position;
		score += Coord.Dot(displacement, start.Fore) * ForwardWeight;
		score += ApproachGainToward(start.Position, state.Position, target) * ApproachWeight;

		if (target is not null
			&& state.Position.ManhattanDistanceTo(target.State.Position) <= TorpedoConfig.BlastRadius)
		{
			score += BlastTargetBonus;
		}

		if (HasAllyInBlast(world, actorId, state.Position))
			score -= BlastAllyPenalty;

		var fuelAfterBurn = System.Math.Max(0, state.FuelRemaining - 1);
		var targetInBlast = target is not null
			&& state.Position.ManhattanDistanceTo(target.State.Position) <= TorpedoConfig.BlastRadius;
		if (fuelAfterBurn == 0 && !targetInBlast)
			score -= WetBoomPenalty;

		return score;
	}

	private static int ApproachGainToward(Coord start, Coord end, Unit? target)
	{
		if (target is null)
			return 0;

		var before = start.ManhattanDistanceTo(target.State.Position);
		var after = end.ManhattanDistanceTo(target.State.Position);
		return after < before ? before - after : 0;
	}

	private static bool HasAllyInBlast(BattleWorld world, string actorId, Coord origin)
	{
		var units = UnitRegistry.For(world);
		var actor = units.UnitOf(actorId);
		foreach (var unit in units.Except(actorId))
		{
			if (!unit.State.IsAlive || actor.RelationTo(unit) != EUnitRelation.Ally)
				continue;
			if (origin.ManhattanDistanceTo(unit.State.Position) <= TorpedoConfig.BlastRadius)
				return true;
		}

		return false;
	}
}
