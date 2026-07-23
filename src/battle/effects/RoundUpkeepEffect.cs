using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class RoundUpkeepEffect : IEffect<BattleBoard, ActorSession>
{
	public void Apply(BattleBoard world, ActorSession runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		var maxAp = actor.Stats.MaxAp;
		if (actor.ApPenaltyNextTurn)
		{
			maxAp = System.Math.Max(0, maxAp - 1);
			actor.ApPenaltyNextTurn = false;
		}

		actor.ActionPoints = maxAp;
		actor.MissilesRemaining = actor.Stats.MissilesPerTurn;
		actor.FlakRemaining = actor.Stats.FlaksPerTurn;
	}
}
