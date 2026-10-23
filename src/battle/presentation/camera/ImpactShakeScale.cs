using Godot;

namespace GrimSpace.Battle.Presentation.Camera;

public readonly record struct ImpactShakeScale(
	int TotalDamage,
	int HullDamage,
	bool TargetDied,
	Vector3 WorldRecoilDirection,
	Vector3? RadialRecoilOriginWorld = null)
{
	public static ImpactShakeScale ForVoidBombDetonation(int deathCount, Vector3 blastOriginWorld) =>
		new(
			TotalDamage: deathCount * 14 + 8,
			HullDamage: deathCount * 10,
			TargetDied: deathCount > 0,
			WorldRecoilDirection: Vector3.Zero,
			RadialRecoilOriginWorld: blastOriginWorld);
}
