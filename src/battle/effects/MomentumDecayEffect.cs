using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Battle.Effects;

public sealed class MomentumDecayEffect : IEffect<BattleSlices>
{
	public static void ApplyTo(State actor) =>
		actor.MomentumLevel = System.Math.Max(actor.MomentumLevel - 1, 0);

	void IEffect<BattleSlices>.Apply(BattleSlices slices) => ApplyTo(slices.Ap.Player);
}
