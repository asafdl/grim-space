namespace GrimSpace.Battle.Player;

public readonly record struct AbilityLegality(WeaponPeek Weapons, bool SpawnRepurposedMiner, bool Detonate)
{
	public static AbilityLegality Empty { get; } = new(WeaponPeek.Empty, false, false);
}
