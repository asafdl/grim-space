using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;
using GrimSpace.Math.Grid;

namespace GrimSpace.Core.Actions.Battle.Effects;

public sealed class MoveEffect(Coord destination) : IEffect<BattleSlices>
{
	public void Apply(State actor) => actor.Position = destination;

	void IEffect<BattleSlices>.Apply(BattleSlices slices) => Apply(slices.Ap.Player);
}
