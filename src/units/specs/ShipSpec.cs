using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Units.Specs;

public abstract class ShipSpec
{
	public abstract EType Chassis { get; }
	public abstract int DefaultMaxHullPoints { get; }
	public abstract FaceShieldPoints DefaultMaxShieldPoints { get; }
	public abstract IReadOnlyList<WeaponSlot> Slots { get; }

	public bool Supports(AbilityMount mount) =>
		Slots.Any(slot => slot.Mount == mount);

	public bool TryGetBaseline(AbilityMount mount, out AbilitySpec? baseline)
	{
		foreach (var slot in Slots)
		{
			if (slot.Mount != mount)
				continue;

			baseline = slot.Baseline;
			return true;
		}

		baseline = null;
		return false;
	}

	public AbilitySpec BaselineFor(AbilityMount mount) =>
		TryGetBaseline(mount, out var baseline) && baseline is not null
			? baseline
			: throw new InvalidOperationException(
				$"Chassis '{Chassis}' has no weapon baseline for mount '{mount.Kind}' / '{mount.Facet}'.");

	public AbilitySpec? TryGetBaselineForKind(EAbilityKind kind)
	{
		foreach (var slot in Slots)
		{
			if (slot.Baseline.Kind == kind)
				return slot.Baseline;
		}

		return null;
	}

	public ShipLoadout NewDefaultLoadout() =>
		ShipLoadout.Create(
			this,
			DefaultMaxHullPoints,
			DefaultMaxShieldPoints,
			Slots.Select(slot => new InstalledAbility(slot.Baseline, slot.Mount.Facet)).ToArray());
}
