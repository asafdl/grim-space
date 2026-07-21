using GrimSpace.Battle.Board;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Weapons;
using GrimSpace.Battle.Actions;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Slices;

public readonly struct HazardContext(BattleBoard board, string actorId)
{
	public void SpawnMissile(string hazardId, Coord center)
	{
		var ownerFrame = BodyFrame.From(board.StateOf(actorId));
		var hazard = Hazard.MissileZone(
			hazardId,
			actorId,
			center,
			ownerFrame,
			board.Grid,
			CombatConfig.MissileRadius,
			CombatConfig.MissileDamage,
			CombatConfig.MissileMomentumLoss);

		board.MutableNonUnits[hazard.Id] = hazard;
	}

	public void SpawnFlakBurst(string hazardId, IEnumerable<Coord> cells)
	{
		var ownerFrame = BodyFrame.From(board.StateOf(actorId));
		var hazard = Hazard.FlakBurst(hazardId, actorId, ownerFrame, cells);
		board.MutableNonUnits[hazard.Id] = hazard;
	}
}
