using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Units;

public sealed record ShipConfiguration(
	EType Chassis,
	int MaxHullPoints,
	FaceShieldPoints Defenses,
	IReadOnlyList<AbilityMount> AbilityMounts)
{
	public ShipConfiguration DeepCopy() =>
		new(
			Chassis,
			MaxHullPoints,
			Defenses.Clone(),
			AbilityMounts.ToArray());

	public static ShipConfiguration Create(
		EType chassis,
		int maxHullPoints,
		FaceShieldPoints defenses,
		IReadOnlyList<AbilityMount> abilityMounts)
	{
		AbilityMount.EnsureUniqueOnShip(abilityMounts);
		return new ShipConfiguration(chassis, maxHullPoints, defenses, abilityMounts);
	}
}
