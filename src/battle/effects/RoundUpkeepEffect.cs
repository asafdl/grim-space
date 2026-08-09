using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class RoundUpkeepEffect : IEffect<BattleWorld, ActorRuntime>
{
	private int _previousActionPoints;
	private int _previousFlakRemaining;
	private int _previousRailgunRemaining;
	private int _previousTorpedoCooldownRemaining;
	private bool _previousApPenaltyNextTurn;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previousActionPoints = actor.ActionPoints;
		_previousFlakRemaining = actor.FlakRemaining;
		_previousRailgunRemaining = actor.RailgunRemaining;
		_previousTorpedoCooldownRemaining = actor.TorpedoCooldownRemaining;
		_previousApPenaltyNextTurn = actor.ApPenaltyNextTurn;

		var maxAp = actor.Stats.MaxAp;
		if (actor.ApPenaltyNextTurn)
		{
			maxAp = System.Math.Max(0, maxAp - 1);
			actor.ApPenaltyNextTurn = false;
		}

		actor.ActionPoints = maxAp;
		actor.FlakRemaining = actor.Stats.FlaksPerTurn;
		actor.RailgunRemaining = actor.Stats.RailgunsPerTurn;
		if (actor.TorpedoCooldownRemaining > 0)
			actor.TorpedoCooldownRemaining--;
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		actor.ActionPoints = _previousActionPoints;
		actor.FlakRemaining = _previousFlakRemaining;
		actor.RailgunRemaining = _previousRailgunRemaining;
		actor.TorpedoCooldownRemaining = _previousTorpedoCooldownRemaining;
		actor.ApPenaltyNextTurn = _previousApPenaltyNextTurn;
	}
}
