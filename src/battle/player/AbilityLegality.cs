namespace GrimSpace.Battle.Player;

public readonly record struct AbilityLegality(
	WeaponPeek Weapons,
	bool SpawnRepurposedMiner,
	bool Detonate,
	bool ShieldsUp,
	bool OverchargeEngines,
	bool Brace,
	bool ProtectionDrone = false)
{
	public static AbilityLegality Empty { get; } = new(WeaponPeek.Empty, false, false, false, false, false);
}
