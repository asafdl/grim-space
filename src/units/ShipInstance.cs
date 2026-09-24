using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
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

	public int MissingHullPoints =>
		System.Math.Max(0, Spec.MaxHullPoints - HullPoints);

	public int TotalCurrentShieldPoints
	{
		get
		{
			var total = 0;
			foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
				total += ShieldPoints[face];
			return total;
		}
	}

	public int TotalMaxShieldPoints
	{
		get
		{
			var total = 0;
			foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
				total += Spec.MaxShieldPoints[face];
			return total;
		}
	}

	public int MissingShieldPoints =>
		System.Math.Max(0, TotalMaxShieldPoints - TotalCurrentShieldPoints);

	public int MissingShieldPointsOnFace(ESpatialOrientation face)
	{
		var max = Spec.MaxShieldPoints[face];
		if (max <= 0)
			return 0;

		var current = System.Math.Clamp(ShieldPoints[face], 0, max);
		return max - current;
	}

	public bool TryWithHullRepaired(out ShipInstance after)
	{
		after = null!;
		if (MissingHullPoints <= 0)
			return false;

		after = new ShipInstance(Id, Spec, Spec.MaxHullPoints, ShieldPoints.Clone());
		return true;
	}

	public bool TryWithShieldsRecharged(out ShipInstance after)
	{
		after = null!;
		if (MissingShieldPoints <= 0)
			return false;

		var shields = ShieldPoints.Clone();
		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
			shields[face] = Spec.MaxShieldPoints[face];

		after = new ShipInstance(Id, Spec, HullPoints, shields);
		return true;
	}

	public bool TryWithShieldsRechargedOnFace(ESpatialOrientation face, out ShipInstance after)
	{
		after = null!;
		if (MissingShieldPointsOnFace(face) <= 0)
			return false;

		var shields = ShieldPoints.Clone();
		shields[face] = Spec.MaxShieldPoints[face];
		after = new ShipInstance(Id, Spec, HullPoints, shields);
		return true;
	}

	public bool TryWithUpgradedMaxShields(out ShipInstance after)
	{
		after = null!;
		try
		{
			var updatedSpec = Spec.WithUpgradedMaxShields();
			var shields = BumpCurrentShields(ShieldPoints, Spec.MaxShieldPoints, updatedSpec.MaxShieldPoints);
			after = new ShipInstance(Id, updatedSpec, HullPoints, shields);
			return true;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
	}

	public bool TryWithUpgradedAbility(AbilityMount mount, out ShipInstance after)
	{
		after = null!;
		var installed = Spec.InstalledAbilities.FirstOrDefault(a => a.Mount == mount);
		if (installed is null || !installed.Spec.TryCreateUpgraded(out var replacement))
			return false;

		var updatedSpec = Spec.WithReplacedMount(mount, replacement);
		after = new ShipInstance(Id, updatedSpec, HullPoints, ShieldPoints.Clone());
		return true;
	}

	private static FaceShieldPoints BumpCurrentShields(
		FaceShieldPoints current,
		FaceShieldPoints previousMax,
		FaceShieldPoints newMax)
	{
		var bumped = current.Clone();
		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
		{
			var delta = newMax[face] - previousMax[face];
			if (delta <= 0)
				continue;

			bumped[face] = System.Math.Min(bumped[face] + delta, newMax[face]);
		}

		return bumped;
	}
}
