using GrimSpace.Battle.Board;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;
using GrimSpace.Math.Grid;

namespace GrimSpace.Core.Actions.Battle.Effects;

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
