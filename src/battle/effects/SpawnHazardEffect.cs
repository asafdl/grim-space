using GrimSpace.Battle.Board;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public sealed class SpawnHazardEffect(
	string hazardId,
	Coord center,
	EHazardKind kind) : IEffect<BattleSlices>
{
	public void Apply(BattleSlices slices)
	{
		switch (kind)
		{
			case EHazardKind.MissileZone:
				slices.Hazards.SpawnMissile(hazardId, center);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(kind));
		}
	}
}
