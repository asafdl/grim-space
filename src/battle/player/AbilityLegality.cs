namespace GrimSpace.Battle.Player;

public readonly record struct AbilityLegality(WeaponPeek Weapons, bool SpawnPatrol)
{
	public static AbilityLegality Empty { get; } = new(WeaponPeek.Empty, false);
}
