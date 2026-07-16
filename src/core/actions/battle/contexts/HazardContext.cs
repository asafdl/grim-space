using GrimSpace.Battle.Board;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Core.Actions.Battle.Contexts;

public readonly struct HazardContext(BattleBoard board, string actorId)
{
	public void SpawnMissile(Coord center)
	{
		var hazard = Hazard.MissileZone(
			board.IdRegistry.NextNonUnitId("missile-zone"),
			actorId,
			center,
			board.Grid,
			CombatConfig.MissileRadius,
			CombatConfig.MissileDamage,
			CombatConfig.MissileMomentumLoss);

		board.MutableNonUnits[hazard.Id] = hazard;
	}
}
