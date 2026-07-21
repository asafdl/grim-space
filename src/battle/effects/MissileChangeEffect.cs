using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Battle.Effects;

public sealed class MissileChangeEffect(int delta) : IEffect<BattleSlices>
{
	public void Apply(MissileContext missiles) => missiles.Change(delta);

	void IEffect<BattleSlices>.Apply(BattleSlices slices) => Apply(slices.Missiles);
}
