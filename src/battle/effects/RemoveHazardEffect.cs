using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Battle.Effects;

public sealed class RemoveHazardEffect(string hazardId) : IEffect<BattleSlices>
{
	public void Apply(BattleSlices slices) => slices.Board.MutableNonUnits.Remove(hazardId);
}
