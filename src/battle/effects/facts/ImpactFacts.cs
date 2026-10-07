using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public readonly record struct ImpactFacts(
	string SourceId,
	string TargetId,
	EImpactCause Cause,
	ESpatialOrientation Face,
	int ShieldDamage,
	int HullDamage)
{
	public int TotalDamage => ShieldDamage + HullDamage;
}
