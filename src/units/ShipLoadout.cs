using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;
using GrimSpace.Units.Specs;

namespace GrimSpace.Units;

public sealed record ShipLoadout(
	int BaseHullPoints,
	FaceShieldPoints BaseShieldPoints,
	IReadOnlyList<InstalledAbility> InstalledAbilities,
	FaceShieldPoints ShieldUpgradeTiers,
	int HullUpgradeTier = 0,
	IReadOnlyList<EChassisBuffOffer>? ChassisBuffs = null,
	int WeaponDamagePercent = 0,
	int OverdriveDamageBonusPercent = 80)
{
	public const int MaxShieldUpgradeTier = 3;
	public const int MaxHullUpgradeTier = 3;
	public const int BaselineOverdriveDamageBonusPercent = 80;

	public IReadOnlyList<EChassisBuffOffer> Buffs => ChassisBuffs ?? [];

	public ShipLoadout DeepCopy() =>
		new(
			BaseHullPoints,
			BaseShieldPoints.Clone(),
			InstalledAbilities.ToArray(),
			ShieldUpgradeTiers.Clone(),
			HullUpgradeTier,
			ChassisBuffs?.ToArray(),
			WeaponDamagePercent,
			OverdriveDamageBonusPercent);

	public ShipLoadout WithInstalledAbility(ShipSpec spec, InstalledAbility installed)
	{
		ArgumentNullException.ThrowIfNull(spec);
		ArgumentNullException.ThrowIfNull(installed);

		var updated = InstalledAbilities.Append(installed).ToArray();
		return Create(
			spec,
			BaseHullPoints,
			BaseShieldPoints,
			updated,
			ShieldUpgradeTiers,
			HullUpgradeTier,
			ChassisBuffs);
	}

	public ShipLoadout WithReplacedMount(ShipSpec spec, AbilityMount mount, InstalledAbility replacement)
	{
		ArgumentNullException.ThrowIfNull(spec);
		ArgumentNullException.ThrowIfNull(replacement);
		if (replacement.Mount != mount)
			throw new ArgumentException(
				$"Replacement mount '{replacement.Mount}' does not match requested mount '{mount}'.",
				nameof(replacement));

		var found = false;
		var updated = InstalledAbilities
			.Select(installed =>
			{
				if (installed.Mount != mount)
					return installed;

				found = true;
				return replacement;
			})
			.ToArray();

		if (!found)
			throw new InvalidOperationException(
				$"Ship has no installed ability on mount '{mount.Kind}' / '{mount.Facet}'.");

		return Create(
			spec,
			BaseHullPoints,
			BaseShieldPoints,
			updated,
			ShieldUpgradeTiers,
			HullUpgradeTier,
			ChassisBuffs);
	}

	public ShipLoadout WithUpgradedBaseShields(ShipSpec spec, ESpatialOrientation face)
	{
		ArgumentNullException.ThrowIfNull(spec);
		if (!Enum.IsDefined(face))
			throw new ArgumentOutOfRangeException(nameof(face));

		var max = BaseShieldPoints.Clone();
		max[face] += 100;
		var tiers = ShieldUpgradeTiers.Clone();
		tiers[face] += 1;

		return Create(
			spec,
			BaseHullPoints,
			max,
			InstalledAbilities,
			tiers,
			HullUpgradeTier,
			ChassisBuffs);
	}

	public ShipLoadout WithMaxShieldUpgradeTier(ShipSpec spec, ESpatialOrientation face, int targetTier)
	{
		ArgumentNullException.ThrowIfNull(spec);
		if (!Enum.IsDefined(face))
			throw new ArgumentOutOfRangeException(nameof(face));

		var currentTier = ShieldUpgradeTiers[face];
		var capacityIncrease = targetTier - currentTier;
		var max = BaseShieldPoints.Clone();
		max[face] += capacityIncrease * 100;
		var tiers = ShieldUpgradeTiers.Clone();
		tiers[face] = targetTier;

		return Create(
			spec,
			BaseHullPoints,
			max,
			InstalledAbilities,
			tiers,
			HullUpgradeTier,
			ChassisBuffs);
	}

	public ShipLoadout WithUpgradedBaseHull(ShipSpec spec)
	{
		ArgumentNullException.ThrowIfNull(spec);
		return Create(
			spec,
			BaseHullPoints + 100,
			BaseShieldPoints,
			InstalledAbilities,
			ShieldUpgradeTiers,
			HullUpgradeTier + 1,
			ChassisBuffs);
	}

	public ShipLoadout WithMaxHullUpgradeTier(ShipSpec spec, int targetTier)
	{
		ArgumentNullException.ThrowIfNull(spec);
		var capacityIncrease = targetTier - HullUpgradeTier;
		return Create(
			spec,
			BaseHullPoints + capacityIncrease * 100,
			BaseShieldPoints,
			InstalledAbilities,
			ShieldUpgradeTiers,
			targetTier,
			ChassisBuffs);
	}

	public static ShipLoadout Create(
		ShipSpec spec,
		int baseHullPoints,
		FaceShieldPoints baseShieldPoints,
		IReadOnlyList<InstalledAbility> installedAbilities,
		FaceShieldPoints? shieldUpgradeTiers = null,
		int hullUpgradeTier = 0,
		IReadOnlyList<EChassisBuffOffer>? chassisBuffs = null)
	{
		ArgumentNullException.ThrowIfNull(spec);
		ArgumentNullException.ThrowIfNull(baseShieldPoints);
		EnsureInstalledCompatible(spec, installedAbilities);
		var tiers = shieldUpgradeTiers?.Clone() ?? new FaceShieldPoints();
		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
		{
			if (tiers[face] < 0 || tiers[face] > MaxShieldUpgradeTier)
				throw new ArgumentOutOfRangeException(nameof(shieldUpgradeTiers), $"Shield upgrade tier on {face} must be between 0 and {MaxShieldUpgradeTier}.");
		}

		var buffs = chassisBuffs?.ToArray() ?? [];
		var bonuses = ChassisBuffCatalog.DamageBonuses(buffs);
		return new ShipLoadout(
			baseHullPoints,
			baseShieldPoints.Clone(),
			installedAbilities,
			tiers,
			hullUpgradeTier,
			buffs,
			bonuses.WeaponDamagePercent,
			bonuses.OverdriveDamageBonusPercent);
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
