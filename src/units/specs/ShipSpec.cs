using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;
using GrimSpace.Units.Maneuvering;

namespace GrimSpace.Units.Specs;

public abstract class ShipSpec
{
	public abstract EType Chassis { get; }
	public abstract int Initiative { get; }
	public abstract int DefaultMaxHullPoints { get; }
	public abstract FaceShieldPoints DefaultMaxShieldPoints { get; }
	public abstract ManeuverabilitySpec Maneuverability { get; }
	public abstract IReadOnlyList<WeaponSlot> Slots { get; }

	public bool Supports(AbilityMount mount) =>
		Slots.Any(slot => slot.Mount == mount);

	public virtual ShipLoadout NewDefaultLoadout() =>
		ShipLoadout.Create(
			this,
			DefaultMaxHullPoints,
			DefaultMaxShieldPoints,
			Slots.Select(slot => new InstalledAbility(slot.Mount.Kind, slot.Mount.Facet)).ToArray());
}
