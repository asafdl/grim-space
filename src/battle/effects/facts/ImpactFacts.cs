using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public readonly record struct ImpactFacts(
	string SourceId,
	string TargetId,
	EHazardKind Cause,
	ESpatialOrientation Face,
	int ShieldDamage,
	int HullDamage,
	int MomentumLoss);
