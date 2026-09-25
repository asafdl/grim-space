using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;
using GrimSpace.Units.Specs;

namespace GrimSpace.Units;

public sealed record ShipLoadout(
	int MaxHullPoints,
	FaceShieldPoints MaxShieldPoints,
	IReadOnlyList<InstalledAbility> InstalledAbilities,
	FaceShieldPoints ShieldUpgradeTiers,
	int HullUpgradeTier = 0)
{
	public const int MaxShieldUpgradeTier = 3;
	public const int MaxHullUpgradeTier = 3;

	public ShipLoadout DeepCopy() =>
		new(
			MaxHullPoints,
			MaxShieldPoints.Clone(),
			InstalledAbilities.ToArray(),
			ShieldUpgradeTiers.Clone(),
			HullUpgradeTier);

	public ShipLoadout WithInstalledAbility(ShipSpec spec, InstalledAbility installed)
	{
		ArgumentNullException.ThrowIfNull(spec);
		ArgumentNullException.ThrowIfNull(installed);

		var updated = InstalledAbilities.Append(installed).ToArray();
		return Create(
			spec,
			MaxHullPoints,
			MaxShieldPoints,
			updated,
			ShieldUpgradeTiers,
			HullUpgradeTier);
	}

	public ShipLoadout WithReplacedMount(ShipSpec spec, AbilityMount mount, AbilitySpec replacement)
	{
		ArgumentNullException.ThrowIfNull(spec);
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
			spec,
			MaxHullPoints,
			MaxShieldPoints,
			updated,
			ShieldUpgradeTiers,
			HullUpgradeTier);
	}

	public ShipLoadout WithUpgradedMaxShields(ShipSpec spec, ESpatialOrientation face)
	{
		ArgumentNullException.ThrowIfNull(spec);
		if (!Enum.IsDefined(face))
			throw new ArgumentOutOfRangeException(nameof(face));
		if (ShieldUpgradeTiers[face] >= MaxShieldUpgradeTier)
			throw new InvalidOperationException($"Ship shields on {face} are already at upgrade tier {ShieldUpgradeTiers[face]}.");

		var max = MaxShieldPoints.Clone();
		max[face] += 1;
		var tiers = ShieldUpgradeTiers.Clone();
		tiers[face] += 1;

		return Create(
			spec,
			MaxHullPoints,
			max,
			InstalledAbilities,
			tiers,
			HullUpgradeTier);
	}

	public ShipLoadout WithUpgradedMaxHull(ShipSpec spec)
	{
		ArgumentNullException.ThrowIfNull(spec);
		if (HullUpgradeTier >= MaxHullUpgradeTier)
			throw new InvalidOperationException($"Ship hull capacity is already at upgrade tier {HullUpgradeTier}.");

		return Create(
			spec,
			MaxHullPoints + 1,
			MaxShieldPoints,
			InstalledAbilities,
			ShieldUpgradeTiers,
			HullUpgradeTier + 1);
	}

	public static ShipLoadout Create(
		ShipSpec spec,
		int maxHullPoints,
		FaceShieldPoints maxShieldPoints,
		IReadOnlyList<InstalledAbility> installedAbilities,
		FaceShieldPoints? shieldUpgradeTiers = null,
		int hullUpgradeTier = 0)
	{
		ArgumentNullException.ThrowIfNull(spec);
		ArgumentNullException.ThrowIfNull(maxShieldPoints);
		EnsureInstalledCompatible(spec, installedAbilities);
		var tiers = shieldUpgradeTiers?.Clone() ?? new FaceShieldPoints();
		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
		{
			if (tiers[face] < 0 || tiers[face] > MaxShieldUpgradeTier)
				throw new ArgumentOutOfRangeException(nameof(shieldUpgradeTiers), $"Shield upgrade tier on {face} must be between 0 and {MaxShieldUpgradeTier}.");
		}

		return new ShipLoadout(
			maxHullPoints,
			maxShieldPoints.Clone(),
			installedAbilities,
			tiers,
			hullUpgradeTier);
	}

	public static void EnsureCompatibleWith(ShipSpec spec, ShipLoadout loadout)
	{
		ArgumentNullException.ThrowIfNull(spec);
		ArgumentNullException.ThrowIfNull(loadout);
		EnsureInstalledCompatible(spec, loadout.InstalledAbilities);
	}

	public static void EnsureInstalledCompatible(ShipSpec spec, IReadOnlyList<InstalledAbility> installed)
	{
		ArgumentNullException.ThrowIfNull(spec);
		InstalledAbility.EnsureValidOnShip(installed);

		foreach (var ability in installed)
		{
			if (!spec.Supports(ability.Mount))
				throw new ArgumentException(
					$"Mount '{ability.Mount.Kind}' / '{ability.Mount.Facet}' is not supported by chassis '{spec.Chassis}'.",
					nameof(installed));
		}
	}
}
