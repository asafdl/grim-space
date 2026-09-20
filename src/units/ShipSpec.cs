using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Units;

public sealed record ShipSpec(
	EType Chassis,
	int MaxHullPoints,
	FaceShieldPoints MaxShieldPoints,
	IReadOnlyList<InstalledAbility> InstalledAbilities,
	TorpedoBodySpec? TorpedoBody = null)
{
	public ShipSpec DeepCopy() =>
		new(Chassis, MaxHullPoints, MaxShieldPoints.Clone(), InstalledAbilities.ToArray(), TorpedoBody);

	public static ShipSpec Create(
		EType chassis,
		int maxHullPoints,
		FaceShieldPoints maxShieldPoints,
		IReadOnlyList<InstalledAbility> installedAbilities,
		TorpedoBodySpec? torpedoBody = null)
	{
		ArgumentNullException.ThrowIfNull(maxShieldPoints);
		InstalledAbility.EnsureValidOnShip(installedAbilities);
		if (chassis == EType.Torpedo && torpedoBody is null)
			throw new ArgumentException("Torpedo chassis requires a torpedo body configuration.", nameof(torpedoBody));
		if (chassis != EType.Torpedo && torpedoBody is not null)
			throw new ArgumentException("Only torpedo chassis may carry a torpedo body configuration.", nameof(torpedoBody));

		return new ShipSpec(
			chassis,
			maxHullPoints,
			maxShieldPoints.Clone(),
			installedAbilities,
			torpedoBody);
	}
}
