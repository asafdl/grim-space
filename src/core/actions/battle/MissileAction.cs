using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Effects;
using GrimSpace.Math.Grid;

using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle;

public sealed class MissileAction(Coord center, EMissileMount mount, int range) : IBattleAction
{
	public Coord Center { get; } = center;
	public EMissileMount Mount { get; } = mount;
	public int Range { get; } = range;

	public bool IsLegal(BattleBoard board, BattlePlanContext context)
	{
		if (context.MissilesRemaining <= 0)
			return false;

		var config = MissileMountConfig.For(Mount).WithRange(Range);
		return MissileTargeting.IsValidTarget(
			board.Player.Position,
			board.Player.ForwardDirection,
			board.Player.RightDirection,
			board.Player.UpDirection,
			Center,
			config,
			board.Grid.IsInBounds);
	}

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board) =>
		[new SpawnHazardEffect(Center)];
}
