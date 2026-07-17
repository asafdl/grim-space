using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Effects;
using GrimSpace.Core.Actions.Battle.Contexts;
using GrimSpace.Math.Grid;

namespace GrimSpace.Core.Actions.Battle;

public sealed class MissileAction(string ownerId, Coord center, EMissileMount mount, int range) : IAction
{
	public string OwnerId { get; } = ownerId;
	public Coord Center { get; } = center;
	public EMissileMount Mount { get; } = mount;
	public int Range { get; } = range;

	public bool IsLegal(BattleBoard board, BattlePlanContext context)
	{
		if (board.StateOf(OwnerId).MissilesRemaining <= 0)
			return false;

		var actor = board.StateOf(OwnerId);
		var config = MissileMountConfig.For(Mount).WithRange(Range);
		return MissileTargeting.IsValidTarget(
			actor.Position,
			actor.ForwardDirection,
			actor.RightDirection,
			actor.UpDirection,
			Center,
			config,
			board.Grid.IsInBounds);
	}

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board, BattlePlanContext context) =>
	[
		new SpawnHazardEffect(Center),
		new MissileChangeEffect(-1),
	];
}
