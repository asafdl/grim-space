using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Units;

public sealed record ShipSpec(
	EType Chassis,
	int MaxHullPoints,
	FaceShieldPoints MaxShieldPoints,
	IReadOnlyList<InstalledAbility> InstalledAbilities,
	FaceShieldPoints ShieldUpgradeTiers,
	int HullUpgradeTier = 0,
	TorpedoBodySpec? TorpedoBody = null)
{
	public const int MaxShieldUpgradeTier = 3;
	public const int MaxHullUpgradeTier = 3;

	public ShipSpec DeepCopy() =>
		new(
			Chassis,
			MaxHullPoints,
			MaxShieldPoints.Clone(),
			InstalledAbilities.ToArray(),
			ShieldUpgradeTiers.Clone(),
			HullUpgradeTier,
			TorpedoBody);

	public ShipSpec WithInstalledAbility(InstalledAbility installed)
	{
		ArgumentNullException.ThrowIfNull(installed);

		var updated = InstalledAbilities.Append(installed).ToArray();
		return Create(
			Chassis,
			MaxHullPoints,
			MaxShieldPoints,
			updated,
			ShieldUpgradeTiers,
			HullUpgradeTier,
			TorpedoBody);
	}

	public ShipSpec WithReplacedMount(AbilityMount mount, AbilitySpec replacement)
	{
		ArgumentNullException.ThrowIfNull(replacement);

		var found = false;
		var updated = InstalledAbilities
			.Select(installed =>
			{
				if (installed.Mount != mount)
					return installed;

				found = true;
				return installed with { Spec = replacement };
			})
			.ToArray();

		if (!found)
			throw new InvalidOperationException(
				$"Ship has no installed ability on mount '{mount.Kind}' / '{mount.Facet}'.");

		return Create(
			Chassis,
			MaxHullPoints,
			MaxShieldPoints,
			updated,
			ShieldUpgradeTiers,
			HullUpgradeTier,
			TorpedoBody);
	}

	public ShipSpec WithUpgradedMaxShields(ESpatialOrientation face)
	{
		if (!Enum.IsDefined(face))
			throw new ArgumentOutOfRangeException(nameof(face));
		if (ShieldUpgradeTiers[face] >= MaxShieldUpgradeTier)
			throw new InvalidOperationException($"Ship shields on {face} are already at upgrade tier {ShieldUpgradeTiers[face]}.");

		var max = MaxShieldPoints.Clone();
		max[face] += 1;
		var tiers = ShieldUpgradeTiers.Clone();
		tiers[face] += 1;

		return Create(
			Chassis,
			MaxHullPoints,
			max,
			InstalledAbilities,
			tiers,
			HullUpgradeTier,
			TorpedoBody);
	}

	public ShipSpec WithUpgradedMaxHull()
	{
		if (HullUpgradeTier >= MaxHullUpgradeTier)
			throw new InvalidOperationException($"Ship hull capacity is already at upgrade tier {HullUpgradeTier}.");

		return Create(
			Chassis,
			MaxHullPoints + 1,
			MaxShieldPoints,
			InstalledAbilities,
			ShieldUpgradeTiers,
			HullUpgradeTier + 1,
			TorpedoBody);
	}

	public static ShipSpec Create(
		EType chassis,
		int maxHullPoints,
		FaceShieldPoints maxShieldPoints,
		IReadOnlyList<InstalledAbility> installedAbilities,
		FaceShieldPoints? shieldUpgradeTiers = null,
		int hullUpgradeTier = 0,
		TorpedoBodySpec? torpedoBody = null)
	{
		ArgumentNullException.ThrowIfNull(maxShieldPoints);
		InstalledAbility.EnsureValidOnShip(installedAbilities);
		var tiers = shieldUpgradeTiers?.Clone() ?? new FaceShieldPoints();
		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
		{
			if (tiers[face] < 0 || tiers[face] > MaxShieldUpgradeTier)
				throw new ArgumentOutOfRangeException(nameof(shieldUpgradeTiers), $"Shield upgrade tier on {face} must be between 0 and {MaxShieldUpgradeTier}.");
		}
		if (chassis == EType.Torpedo && torpedoBody is null)
			throw new ArgumentException("Torpedo chassis requires a torpedo body configuration.", nameof(torpedoBody));
		if (chassis != EType.Torpedo && torpedoBody is not null)
			throw new ArgumentException("Only torpedo chassis may carry a torpedo body configuration.", nameof(torpedoBody));

		return new ShipSpec(
			chassis,
			maxHullPoints,
			maxShieldPoints.Clone(),
			installedAbilities,
			tiers,
			hullUpgradeTier,
			torpedoBody);
	}
}
