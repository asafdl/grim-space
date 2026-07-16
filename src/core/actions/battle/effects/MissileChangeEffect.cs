using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle.Effects;

public sealed class MissileChangeEffect(int delta) : IEffect<BattleSlices>
{
	public void Apply(MissileContext missiles) => missiles.Change(delta);

	void IEffect<BattleSlices>.Apply(BattleSlices slices) => Apply(slices.Missiles);
}
