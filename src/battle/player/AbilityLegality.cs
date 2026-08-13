namespace GrimSpace.Battle.Player;

public readonly record struct AbilityLegality(WeaponPeek Weapons, bool SpawnPatrol, bool Detonate)
{
	public static AbilityLegality Empty { get; } = new(WeaponPeek.Empty, false, false);
}
