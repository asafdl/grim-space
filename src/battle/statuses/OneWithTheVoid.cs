using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Statuses;

public sealed class OneWithTheVoid : BattleStatus
{
	public override EStatusPolarity Polarity => EStatusPolarity.Buff;

	public static void CopyOnto(State parent, State child)
	{
		if (!parent.Has<OneWithTheVoid>() || child.Has<OneWithTheVoid>())
			return;
		child.Statuses.Add(new OneWithTheVoid());
	}
}
