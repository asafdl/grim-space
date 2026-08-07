using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Battle.World;
using GrimSpace.Core.Dfs;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Ai;

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
		var actor = session.World.UnitOf(actorId);
		Unit? best = null;
		var bestClass = ETorpedoTargetClass.Unreachable;
		var bestTurn = int.MaxValue;
		var bestDistance = int.MaxValue;

		foreach (var unit in session.World.UnitsExcept(actorId))
		{
			if (!unit.State.IsAlive
				|| unit.Controller == actor.Controller
				|| unit.State.Type == EType.Torpedo)
			{
				continue;
			}

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

	public static int ScoreHeuristic(
		SearchFrame<BattleWorld, ActorRuntime> frame,
		BattleSimulation anchor,
		string actorId,
		Unit? target,
		int searchStartDepth)
	{
		var world = frame.World;
		if (!world.Units.TryGetValue(actorId, out var unit) || !unit.State.IsAlive)
			return int.MinValue;

		var state = unit.State;
		var start = anchor.ReplayWorld(searchStartDepth).StateOf(actorId);
		var score = -state.ActionPoints * UnusedApPenalty;

		var displacement = state.Position - start.Position;
		score += Coord.Dot(displacement, start.Fore) * ForwardWeight;

		if (target is not null)
		{
			var before = start.Position.ManhattanDistanceTo(target.State.Position);
			var after = state.Position.ManhattanDistanceTo(target.State.Position);
			if (after < before)
				score += (before - after) * ApproachWeight;

			if (after <= TorpedoConfig.BlastRadius)
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

	private static bool HasAllyInBlast(BattleWorld world, string actorId, Coord origin)
	{
		var actor = world.UnitOf(actorId);
		foreach (var unit in world.UnitsExcept(actorId))
		{
			if (!unit.State.IsAlive || unit.Controller != actor.Controller)
				continue;
			if (origin.ManhattanDistanceTo(unit.State.Position) <= TorpedoConfig.BlastRadius)
				return true;
		}

		return false;
	}
}
