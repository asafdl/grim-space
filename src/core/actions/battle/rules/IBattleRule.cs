namespace GrimSpace.Core.Actions.Battle.Rules;

public interface IBattleRule
{
	bool IsSatisfied(BattleBoard board, BattlePlanContext context);
}
