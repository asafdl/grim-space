using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Units;

public sealed class ShipInstance
{
	public string Id { get; }
	public ShipSpec Spec { get; set; }
	public int HullPoints { get; set; }
	public FaceShieldPoints ShieldPoints { get; set; }

	public ShipInstance(
		string id,
		ShipSpec spec,
		int hullPoints,
		FaceShieldPoints shieldPoints)
	{
		ArgumentException.ThrowIfNullOrEmpty(id);
		ArgumentNullException.ThrowIfNull(spec);
		ArgumentNullException.ThrowIfNull(shieldPoints);
		Id = id;
		Spec = spec;
		HullPoints = hullPoints;
		ShieldPoints = shieldPoints;
	}

	public static ShipInstance FromCatalog(string id, EType chassis) =>
		FromSpec(id, ShipCatalog.DefaultFor(chassis));

	public static ShipInstance FromSpec(string id, ShipSpec spec) =>
		new(id, spec, spec.MaxHullPoints, spec.MaxShieldPoints.Clone());

	public ShipInstance Clone() =>
		new(Id, Spec.DeepCopy(), HullPoints, ShieldPoints.Clone());
}
