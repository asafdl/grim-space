using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public sealed class MoveEffect(Coord destination) : IEffect<BattleSlices>
{
	public void Apply(State actor) => actor.Position = destination;

	void IEffect<BattleSlices>.Apply(BattleSlices slices) => Apply(slices.Ap.Player);
}
