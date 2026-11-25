using GrimSpace.Battle.Effects;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Statuses;

public sealed class ProtectionDroneBuff : BattleStatus
{
	public override EStatusPolarity Polarity => EStatusPolarity.Buff;

	public sealed class Rule : TargetDamageReduction<ProtectionDroneBuff>
	{
		protected override int RetainPercent => ProtectionDroneSpec.Baseline.DamageTakenPercent;
	}
}
