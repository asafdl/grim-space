using GrimSpace.Battle.Actions.Contexts;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Actions.Effects;

public sealed class SpawnHazardEffect(Coord center) : IStateEffect
{
	public void Apply(HazardContext hazards) => hazards.SpawnMissile(center);

	void IStateEffect.Apply(ActionSlices slices) => Apply(slices.Hazards);
}
