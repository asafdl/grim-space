using GrimSpace.Math.Grid;
using GrimSpace.Battle.Weapons;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Core.Actions.Battle.Contexts;

public readonly struct HazardContext(ICollection<Hazard> hazards, BoundedGrid grid)
{
	public void SpawnMissile(Coord center)
	{
		hazards.Add(Hazard.MissileZone(
			center,
			grid,
			CombatConfig.MissileRadius,
			CombatConfig.MissileDamage,
			CombatConfig.MissileMomentumLoss));
	}
}
