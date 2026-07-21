using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public sealed class SpawnFlakHazardEffect(string hazardId, IEnumerable<Coord> cells) : IEffect<BattleSlices>
{
	public void Apply(BattleSlices slices) => slices.Hazards.SpawnFlakBurst(hazardId, cells);
}
