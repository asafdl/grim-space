using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;
using GrimSpace.Math.Grid;

namespace GrimSpace.Core.Actions.Battle.Effects;

public sealed class SpawnFlakHazardEffect(string hazardId, IEnumerable<Coord> cells) : IEffect<BattleSlices>
{
	public void Apply(BattleSlices slices) => slices.Hazards.SpawnFlakBurst(hazardId, cells);
}
