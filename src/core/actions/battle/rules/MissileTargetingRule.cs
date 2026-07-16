using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;

namespace GrimSpace.Core.Actions.Battle.Rules;

public sealed class MissileTargetingRule(
	string actorId,
	Coord center,
	EMissileMount mount,
	int range) : IBattleRule
{
	public bool IsSatisfied(BattleBoard board, BattlePlanContext context)
	{
		var actor = board.StateOf(actorId);
		var config = MissileMountConfig.For(mount).WithRange(range);
		return MissileTargeting.IsValidTarget(
			actor.Position,
			actor.ForwardDirection,
			actor.RightDirection,
			actor.UpDirection,
			center,
			config,
			board.Grid.IsInBounds);
	}
}
