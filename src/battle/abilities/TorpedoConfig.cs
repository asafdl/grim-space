namespace GrimSpace.Battle.Abilities;

public enum ETorpedoMount
{
	Aft,
	Ventral,
	Dorsal,
}

public static class TorpedoConfig
{
	public const int Fuel = 3;
	public const int CooldownTurns = 3;
	public const int BlastRadius = 4;
	public const int BlastDamage = 3;
	public const int SpawnMomentum = 3;

	public static readonly ETorpedoMount[] EnabledMounts =
	[
		ETorpedoMount.Aft,
		ETorpedoMount.Ventral,
		ETorpedoMount.Dorsal,
	];
}
