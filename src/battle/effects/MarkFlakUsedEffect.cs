using GrimSpace.Battle.Slices;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class MarkFlakUsedEffect : IEffect<BattleSlices>
{
	public void Apply(BattleSlices slices) => slices.PhaseContext.FlakUsedThisTurn = true;
}
