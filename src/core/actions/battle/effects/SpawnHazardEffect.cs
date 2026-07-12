using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;
using GrimSpace.Math.Grid;

namespace GrimSpace.Core.Actions.Battle.Effects;

public sealed class SpawnHazardEffect(Coord center) : IEffect<BattleSlices>
{
	public void Apply(HazardContext hazards) => hazards.SpawnMissile(center);

	void IEffect<BattleSlices>.Apply(BattleSlices slices) => Apply(slices.Hazards);
}
