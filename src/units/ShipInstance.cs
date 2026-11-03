using GrimSpace.Math.Grid;
using GrimSpace.Run.Items;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;
using GrimSpace.Units.Specs;

namespace GrimSpace.Units;

public sealed class ShipInstance : IItem
{
	public string Id { get; }
	public ShipSpec Spec { get; }
	public ShipLoadout Loadout { get; set; }
	public int CurrentHullPoints { get; set; }
	public FaceShieldPoints CurrentShieldPoints { get; set; }

	public ShipInstance(
		string id,
		ShipSpec spec,
		ShipLoadout loadout,
		int currentHullPoints,
		FaceShieldPoints currentShieldPoints)
	{
		ArgumentException.ThrowIfNullOrEmpty(id);
		ArgumentNullException.ThrowIfNull(spec);
		ArgumentNullException.ThrowIfNull(loadout);
		ArgumentNullException.ThrowIfNull(currentShieldPoints);
		ShipLoadout.EnsureCompatibleWith(spec, loadout);
		Id = id;
		Spec = spec;
		Loadout = loadout;
		CurrentHullPoints = currentHullPoints;
		CurrentShieldPoints = currentShieldPoints;
	}

	public static ShipInstance FromCatalog(string id, EType chassis) =>
		ShipInstance.FromSpec(id, ShipCatalog.SpecFor(chassis), ShipCatalog.LoadoutForTier(chassis, EShipGearTier.T0));

	public static ShipInstance FromSpec(string id, ShipSpec spec, ShipLoadout loadout) =>
		new(id, spec, loadout, loadout.BaseHullPoints, loadout.BaseShieldPoints.Clone());

	public ShipInstance Clone() =>
		new(Id, Spec, Loadout.DeepCopy(), CurrentHullPoints, CurrentShieldPoints.Clone());

	public int MissingHullPoints =>
		System.Math.Max(0, Loadout.BaseHullPoints - CurrentHullPoints);

	public int TotalCurrentShieldPoints
	{
		get
		{
			var total = 0;
			foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
				total += CurrentShieldPoints[face];
			return total;
		}
	}

	public int TotalBaseShieldPoints
	{
		get
		{
			var total = 0;
			foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
				total += Loadout.BaseShieldPoints[face];
			return total;
		}
	}

	public int MissingShieldPoints =>
		System.Math.Max(0, TotalBaseShieldPoints - TotalCurrentShieldPoints);

	public int MissingShieldPointsOnFace(ESpatialOrientation face)
	{
		var max = Loadout.BaseShieldPoints[face];
		if (max <= 0)
			return 0;

		var current = System.Math.Clamp(CurrentShieldPoints[face], 0, max);
		return max - current;
	}

	public bool TryWithHullRepaired(out ShipInstance after)
	{
		after = null!;
		if (MissingHullPoints <= 0)
			return false;

		after = new ShipInstance(Id, Spec, Loadout, Loadout.BaseHullPoints, CurrentShieldPoints.Clone());
		return true;
	}

	public bool TryWithShieldsRecharged(out ShipInstance after)
	{
		after = null!;
		if (MissingShieldPoints <= 0)
			return false;

		var shields = CurrentShieldPoints.Clone();
		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
			shields[face] = Loadout.BaseShieldPoints[face];

		after = new ShipInstance(Id, Spec, Loadout, CurrentHullPoints, shields);
		return true;
	}

	public bool TryWithShieldsRechargedOnFace(ESpatialOrientation face, out ShipInstance after)
	{
		after = null!;
		if (MissingShieldPointsOnFace(face) <= 0)
			return false;

		var shields = CurrentShieldPoints.Clone();
		shields[face] = Loadout.BaseShieldPoints[face];
		after = new ShipInstance(Id, Spec, Loadout, CurrentHullPoints, shields);
		return true;
	}

	public bool TryWithUpgradedBaseShields(ESpatialOrientation face, out ShipInstance after)
	{
		after = null!;
		if (!Enum.IsDefined(face) || Loadout.ShieldUpgradeTiers[face] >= ShipLoadout.MaxShieldUpgradeTier)
			return false;

		var updatedLoadout = Loadout.WithUpgradedBaseShields(Spec, face);
		var shields = CurrentShieldPoints.Clone();
		shields[face] = updatedLoadout.BaseShieldPoints[face];
		after = new ShipInstance(Id, Spec, updatedLoadout, CurrentHullPoints, shields);
		return true;
	}

	public bool TryWithUpgradedBaseHull(out ShipInstance after)
	{
		after = null!;
		if (Loadout.HullUpgradeTier >= ShipLoadout.MaxHullUpgradeTier)
			return false;

		var updatedLoadout = Loadout.WithUpgradedBaseHull(Spec);
		after = new ShipInstance(
			Id,
			Spec,
			updatedLoadout,
			updatedLoadout.BaseHullPoints,
			CurrentShieldPoints.Clone());
		return true;
	}

	public bool TryWithInstalledAbility(InstalledAbility installed, out ShipInstance after)
	{
		after = null!;
		ArgumentNullException.ThrowIfNull(installed);
		try
		{
			var updatedLoadout = Loadout.WithInstalledAbility(Spec, installed);
			after = new ShipInstance(Id, Spec, updatedLoadout, CurrentHullPoints, CurrentShieldPoints.Clone());
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
		if (installed is null || installed.DamageUpgradeTier >= installed.Spec.MaxDamageUpgrades)
			return false;

		var updatedLoadout = Loadout.WithReplacedMount(
			Spec,
			mount,
			installed.WithDamageUpgradeTier(installed.DamageUpgradeTier + 1));
		after = new ShipInstance(Id, Spec, updatedLoadout, CurrentHullPoints, CurrentShieldPoints.Clone());
		return true;
	}

	public bool TryWithRangeUpgraded(AbilityMount mount, out ShipInstance after)
	{
		after = null!;
		var installed = Loadout.InstalledAbilities.FirstOrDefault(a => a.Mount == mount);
		if (installed is null || installed.RangeUpgradeTier >= installed.Spec.MaxRangeUpgrades)
			return false;

		var updatedLoadout = Loadout.WithReplacedMount(
			Spec,
			mount,
			installed.WithRangeUpgradeTier(installed.RangeUpgradeTier + 1));
		after = new ShipInstance(Id, Spec, updatedLoadout, CurrentHullPoints, CurrentShieldPoints.Clone());
		return true;
	}

	public bool TryWithDamageUpgradeTier(AbilityMount mount, int targetTier, out ShipInstance after)
	{
		after = null!;
		var installed = Loadout.InstalledAbilities.FirstOrDefault(a => a.Mount == mount);
		if (installed is null
			|| targetTier <= installed.DamageUpgradeTier
			|| targetTier > installed.Spec.MaxDamageUpgrades)
			return false;

		var updatedLoadout = Loadout.WithReplacedMount(
			Spec,
			mount,
			installed.WithDamageUpgradeTier(targetTier));
		after = new ShipInstance(Id, Spec, updatedLoadout, CurrentHullPoints, CurrentShieldPoints.Clone());
		return true;
	}

	public bool TryWithRangeUpgradeTier(AbilityMount mount, int targetTier, out ShipInstance after)
	{
		after = null!;
		var installed = Loadout.InstalledAbilities.FirstOrDefault(a => a.Mount == mount);
		if (installed is null
			|| targetTier <= installed.RangeUpgradeTier
			|| targetTier > installed.Spec.MaxRangeUpgrades)
			return false;

		var updatedLoadout = Loadout.WithReplacedMount(
			Spec,
			mount,
			installed.WithRangeUpgradeTier(targetTier));
		after = new ShipInstance(Id, Spec, updatedLoadout, CurrentHullPoints, CurrentShieldPoints.Clone());
		return true;
	}

	public bool TryWithMaxHullUpgradeTier(int targetTier, out ShipInstance after)
	{
		after = null!;
		if (targetTier <= Loadout.HullUpgradeTier || targetTier > ShipLoadout.MaxHullUpgradeTier)
			return false;

		var updatedLoadout = Loadout.WithMaxHullUpgradeTier(Spec, targetTier);
		after = new ShipInstance(
			Id,
			Spec,
			updatedLoadout,
			updatedLoadout.BaseHullPoints,
			CurrentShieldPoints.Clone());
		return true;
	}

	public bool TryWithMaxShieldUpgradeTier(ESpatialOrientation face, int targetTier, out ShipInstance after)
	{
		after = null!;
		if (!Enum.IsDefined(face))
			return false;

		var currentTier = Loadout.ShieldUpgradeTiers[face];
		if (targetTier <= currentTier || targetTier > ShipLoadout.MaxShieldUpgradeTier)
			return false;

		var updatedLoadout = Loadout.WithMaxShieldUpgradeTier(Spec, face, targetTier);
		var shields = CurrentShieldPoints.Clone();
		shields[face] = updatedLoadout.BaseShieldPoints[face];
		after = new ShipInstance(Id, Spec, updatedLoadout, CurrentHullPoints, shields);
		return true;
	}

	public bool TryWithMaxShieldPoints(
		ESpatialOrientation face,
		int targetMaxShieldPoints,
		out ShipInstance after)
	{
		after = null!;
		if (!Enum.IsDefined(face) || targetMaxShieldPoints <= Loadout.BaseShieldPoints[face])
			return false;

		var currentTier = Loadout.ShieldUpgradeTiers[face];
		var baselineMaxShieldPoints = Loadout.BaseShieldPoints[face] - currentTier * 100;
		var targetTier = (targetMaxShieldPoints - baselineMaxShieldPoints) / 100;
		if (targetTier <= currentTier || targetTier > ShipLoadout.MaxShieldUpgradeTier)
			return false;

		var updatedLoadout = Loadout.WithMaxShieldUpgradeTier(Spec, face, targetTier);
		var shields = CurrentShieldPoints.Clone();
		shields[face] = updatedLoadout.BaseShieldPoints[face];
		after = new ShipInstance(Id, Spec, updatedLoadout, CurrentHullPoints, shields);
		return true;
	}

}
