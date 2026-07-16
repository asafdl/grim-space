namespace GrimSpace.Core.Actions.Battle.Rules;

public sealed class HasMissileAmmoRule(string actorId) : IBattleRule
{
	public bool IsSatisfied(BattleBoard board, BattlePlanContext context) =>
		board.StateOf(actorId).MissilesRemaining > 0;
}
