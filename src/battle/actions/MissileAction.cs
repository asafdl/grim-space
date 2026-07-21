using GrimSpace.Battle.Board;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Slices;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Actions;

public sealed class MissileAction(
	string ownerId,
	Coord center,
	EMissileMount mount,
	int range,
	int? undoGroup = null) : IAction
{
	public string OwnerId { get; } = ownerId;
	public int? UndoGroup { get; } = undoGroup;
	public Coord Center { get; } = center;
	public EMissileMount Mount { get; } = mount;
	public int Range { get; } = range;

	public bool IsLegal(BattleBoard board, BattlePlanContext context)
	{
		if (board.StateOf(OwnerId).MissilesRemaining <= 0)
			return false;

		var frame = BodyFrame.From(board.StateOf(OwnerId));
		var config = MissileMountConfig.For(Mount).WithRange(Range);
		return MissileTargeting.IsValidTarget(frame, Center, config, board.Grid.IsInBounds);
	}

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board, BattlePlanContext context)
	{
		var hazardId = board.IdRegistry.NextNonUnitId("missile-zone");
		return
		[
			new SpawnHazardEffect(hazardId, Center, EHazardKind.MissileZone),
			new ScheduleActionEffect(
				CombatConfig.MissileResolveDelay,
				new ResolveHazardAction(OwnerId, hazardId)),
			new MissileChangeEffect(-1),
		];
	}
}
