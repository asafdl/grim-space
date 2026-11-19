namespace GrimSpace.Battle.Presentation.Replay;

internal static class ReplayTiming
{
	public const double MoveStepSeconds = 0.14;
	/// <summary>Experiment knob. Scales enemy-phase move, turn, and impact beats. 1 restores the old timing.</summary>
	public const double EnemyPace = 1.6;
	public const double EnemyActivationHoldSeconds = 0.45;
	public const double OrientationSeconds = 0.12;
	public const double ImpactPauseSeconds = 0.52;
	public const double WeaponBurstSeconds = 0.48;
	public const double DeathExplosionSeconds = 1.25;
}
