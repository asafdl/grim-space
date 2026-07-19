using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle.Effects;

public sealed class RemoveHazardEffect(string hazardId) : IEffect<BattleSlices>
{
	public void Apply(BattleSlices slices) => slices.Board.MutableNonUnits.Remove(hazardId);
}
