using GrimSpace.Math.Grid;

namespace GrimSpace.Units.Loadouts.Abilities;

/// <summary>
/// One ability installed on one ship facet.
/// </summary>
public sealed record InstalledAbility
{
	public EAbilityKind Kind { get; }
	public ESpatialOrientation MountedOn { get; }
	public int DamageUpgradeTier { get; }
	public int RangeUpgradeTier { get; }
	public AbilitySpec Spec => AbilitySpec.Create(Kind, DamageUpgradeTier, RangeUpgradeTier);
	public AbilityMount Mount => new(Kind, MountedOn);

	public InstalledAbility(
		EAbilityKind kind,
		ESpatialOrientation mountedOn,
		int damageUpgradeTier = 0,
		int rangeUpgradeTier = 0)
	{
		_ = AbilitySpec.Create(kind, damageUpgradeTier, rangeUpgradeTier);
		Kind = kind;
		MountedOn = mountedOn;
		DamageUpgradeTier = damageUpgradeTier;
		RangeUpgradeTier = rangeUpgradeTier;
	}

	public InstalledAbility(AbilitySpec effectiveSpec, ESpatialOrientation mountedOn)
	{
		ArgumentNullException.ThrowIfNull(effectiveSpec);
		for (var damage = 0; damage <= effectiveSpec.MaxDamageUpgrades; damage++)
		{
			for (var range = 0; range <= effectiveSpec.MaxRangeUpgrades; range++)
			{
				if (AbilitySpec.Create(effectiveSpec.Kind, damage, range) != effectiveSpec)
					continue;

				Kind = effectiveSpec.Kind;
				MountedOn = mountedOn;
				DamageUpgradeTier = damage;
				RangeUpgradeTier = range;
				return;
			}
		}

		throw new ArgumentException(
			$"Ability spec '{effectiveSpec.Kind}' is not a canonical baseline with valid upgrades.",
			nameof(effectiveSpec));
	}

	public InstalledAbility WithDamageUpgrade() =>
		new(Kind, MountedOn, DamageUpgradeTier + 1, RangeUpgradeTier);

	public InstalledAbility WithRangeUpgrade() =>
		new(Kind, MountedOn, DamageUpgradeTier, RangeUpgradeTier + 1);

	public static void EnsureValidOnShip(IReadOnlyList<InstalledAbility> installed)
	{
		ArgumentNullException.ThrowIfNull(installed);
		if (installed.Count == 0)
			return;

		var mounts = new HashSet<AbilityMount>();
		foreach (var ability in installed)
		{
			if (!mounts.Add(ability.Mount))
				throw new ArgumentException(
					$"Ability '{ability.Kind}' is already installed on facet '{ability.MountedOn}'.",
					nameof(installed));

		}
	}
}
