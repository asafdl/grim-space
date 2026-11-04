using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public readonly record struct ImpactFacts(
	string SourceId,
	string TargetId,
	EImpactCause Cause,
	ESpatialOrientation Face,
	int ShieldDamage,
	int HullDamage,
	int ShieldBefore,
	int HullBefore)
{
	public int TotalDamage => ShieldDamage + HullDamage;
	public bool IsLethal => HullBefore > 0 && HullDamage >= HullBefore;
}
