namespace GrimSpace.Core.Actions.Battle.Rules;

public static class BattleRuleEnforcer
{
	public static bool AllSatisfied(
		IEnumerable<IBattleRule> rules,
		BattleBoard board,
		BattlePlanContext context) =>
		rules.All(rule => rule.IsSatisfied(board, context));
}
