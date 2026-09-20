using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Units;

public sealed class ShipInstance
{
	public string Id { get; }
	public ShipSpec Spec { get; set; }
	public int HullPoints { get; set; }
	public FaceShieldPoints MaxShieldPoints { get; set; }
	public FaceShieldPoints ShieldPoints { get; set; }

	public ShipInstance(
		string id,
		ShipSpec spec,
		int hullPoints,
		FaceShieldPoints maxShieldPoints,
		FaceShieldPoints shieldPoints)
	{
		ArgumentException.ThrowIfNullOrEmpty(id);
		ArgumentNullException.ThrowIfNull(spec);
		ArgumentNullException.ThrowIfNull(maxShieldPoints);
		ArgumentNullException.ThrowIfNull(shieldPoints);
		Id = id;
		Spec = spec;
		HullPoints = hullPoints;
		MaxShieldPoints = maxShieldPoints;
		ShieldPoints = shieldPoints;
	}

	public static ShipInstance FromCatalog(string id, EType chassis)
	{
		var spec = ShipCatalog.DefaultFor(chassis);
		var maxShields = ShipCatalog.MaxShieldPointsFor(chassis).Clone();
		return new ShipInstance(id, spec, spec.MaxHullPoints, maxShields, maxShields.Clone());
	}

	public static ShipInstance FromSpec(string id, ShipSpec spec)
	{
		var maxShields = ShipCatalog.MaxShieldPointsFor(spec.Chassis).Clone();
		return new ShipInstance(id, spec, spec.MaxHullPoints, maxShields, maxShields.Clone());
	}

	public ShipInstance Clone() =>
		new(Id, Spec.DeepCopy(), HullPoints, MaxShieldPoints.Clone(), ShieldPoints.Clone());
}
