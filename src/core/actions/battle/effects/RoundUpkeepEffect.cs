using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle.Effects;

public sealed class RoundUpkeepEffect : IEffect<BattleSlices>
{
	public void Apply(State actor)
	{
		var maxAp = actor.Stats.MaxAp;
		if (actor.ApPenaltyNextTurn)
		{
			maxAp = System.Math.Max(0, maxAp - 1);
			actor.ApPenaltyNextTurn = false;
		}

		actor.ActionPoints = maxAp;
		actor.MissilesRemaining = actor.Stats.MissilesPerTurn;
	}

	void IEffect<BattleSlices>.Apply(BattleSlices slices) => Apply(slices.Ap.Player);
}
