using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;
using GrimSpace.Units.Specs;

namespace GrimSpace.Units;

public sealed class ShipInstance
{
	public string Id { get; }
	public ShipSpec Spec { get; }
	public ShipLoadout Loadout { get; set; }
	public int HullPoints { get; set; }
	public FaceShieldPoints ShieldPoints { get; set; }

	public ShipInstance(
		string id,
		ShipSpec spec,
		ShipLoadout loadout,
		int hullPoints,
		FaceShieldPoints shieldPoints)
	{
		ArgumentException.ThrowIfNullOrEmpty(id);
		ArgumentNullException.ThrowIfNull(spec);
		ArgumentNullException.ThrowIfNull(loadout);
		ArgumentNullException.ThrowIfNull(shieldPoints);
		ShipLoadout.EnsureCompatibleWith(spec, loadout);
		Id = id;
		Spec = spec;
		Loadout = loadout;
		HullPoints = hullPoints;
		ShieldPoints = shieldPoints;
	}

	public static ShipInstance FromCatalog(string id, EType chassis) =>
		ShipInstance.FromSpec(id, ShipCatalog.SpecFor(chassis), ShipCatalog.SpecFor(chassis).NewDefaultLoadout());

	public static ShipInstance FromSpec(string id, ShipSpec spec, ShipLoadout loadout) =>
		new(id, spec, loadout, loadout.MaxHullPoints, loadout.MaxShieldPoints.Clone());

	public ShipInstance Clone() =>
		new(Id, Spec, Loadout.DeepCopy(), HullPoints, ShieldPoints.Clone());

	public int MissingHullPoints =>
		System.Math.Max(0, Loadout.MaxHullPoints - HullPoints);

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
				total += Loadout.MaxShieldPoints[face];
			return total;
		}
	}

	public int MissingShieldPoints =>
		System.Math.Max(0, TotalMaxShieldPoints - TotalCurrentShieldPoints);

	public int MissingShieldPointsOnFace(ESpatialOrientation face)
	{
		var max = Loadout.MaxShieldPoints[face];
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

		after = new ShipInstance(Id, Spec, Loadout, Loadout.MaxHullPoints, ShieldPoints.Clone());
		return true;
	}

	public bool TryWithShieldsRecharged(out ShipInstance after)
	{
		after = null!;
		if (MissingShieldPoints <= 0)
			return false;

		var shields = ShieldPoints.Clone();
		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
			shields[face] = Loadout.MaxShieldPoints[face];

		after = new ShipInstance(Id, Spec, Loadout, HullPoints, shields);
		return true;
	}

	public bool TryWithShieldsRechargedOnFace(ESpatialOrientation face, out ShipInstance after)
	{
		after = null!;
		if (MissingShieldPointsOnFace(face) <= 0)
			return false;

		var shields = ShieldPoints.Clone();
		shields[face] = Loadout.MaxShieldPoints[face];
		after = new ShipInstance(Id, Spec, Loadout, HullPoints, shields);
		return true;
	}

	public bool TryWithUpgradedMaxShields(ESpatialOrientation face, out ShipInstance after)
	{
		after = null!;
		if (!Enum.IsDefined(face) || Loadout.ShieldUpgradeTiers[face] >= ShipLoadout.MaxShieldUpgradeTier)
			return false;
		try
		{
			var updatedLoadout = Loadout.WithUpgradedMaxShields(Spec, face);
			var shields = BumpCurrentShields(ShieldPoints, Loadout.MaxShieldPoints, updatedLoadout.MaxShieldPoints);
			after = new ShipInstance(Id, Spec, updatedLoadout, HullPoints, shields);
			return true;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
	}

	public bool TryWithUpgradedMaxHull(out ShipInstance after)
	{
		after = null!;
		try
		{
			var updatedLoadout = Loadout.WithUpgradedMaxHull(Spec);
			after = new ShipInstance(Id, Spec, updatedLoadout, HullPoints, ShieldPoints.Clone());
			return true;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
	}

	public bool TryWithInstalledAbility(InstalledAbility installed, out ShipInstance after)
	{
		after = null!;
		ArgumentNullException.ThrowIfNull(installed);
		try
		{
			var updatedLoadout = Loadout.WithInstalledAbility(Spec, installed);
			after = new ShipInstance(Id, Spec, updatedLoadout, HullPoints, ShieldPoints.Clone());
			return true;
		}
		catch (ArgumentException)
		{
			return false;
		}
	}

	public bool TryWithDamageUpgraded(AbilityMount mount, out ShipInstance after)
	{
		after = null!;
		var installed = Loadout.InstalledAbilities.FirstOrDefault(a => a.Mount == mount);
		if (installed is null || !installed.Spec.TryCreateDamageUpgraded(out var replacement))
			return false;

		var updatedLoadout = Loadout.WithReplacedMount(Spec, mount, replacement);
		after = new ShipInstance(Id, Spec, updatedLoadout, HullPoints, ShieldPoints.Clone());
		return true;
	}

	public bool TryWithRangeUpgraded(AbilityMount mount, out ShipInstance after)
	{
		after = null!;
		var installed = Loadout.InstalledAbilities.FirstOrDefault(a => a.Mount == mount);
		if (installed is null || !installed.Spec.TryCreateRangeUpgraded(out var replacement))
			return false;

		var updatedLoadout = Loadout.WithReplacedMount(Spec, mount, replacement);
		after = new ShipInstance(Id, Spec, updatedLoadout, HullPoints, ShieldPoints.Clone());
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
