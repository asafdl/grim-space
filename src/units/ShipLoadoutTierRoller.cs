using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Units;

internal static class ShipLoadoutTierRoller
{
	public const int CostInstallWeapon = 5;
	public const int CostWeaponDamage = 4;
	public const int CostHullMax = 3;
	public const int CostWeaponRange = 2;
	public const int CostShieldMax = 1;

	public static int PowerBudget(EShipGearTier tier) =>
		tier switch
		{
			EShipGearTier.T0 => 0,
			EShipGearTier.T1 => 4,
			EShipGearTier.T2 => 8,
			EShipGearTier.T3 => 12,
			_ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null),
		};

	public static ShipLoadout Roll(EType chassis, EShipGearTier tier, int? rollSeed, ShipLoadout baseline)
	{
		var remaining = PowerBudget(tier);
		if (remaining <= 0)
			return baseline.DeepCopy();

		var spec = ShipCatalog.SpecFor(chassis);
		var ship = ShipInstance.FromSpec("__tier_roll__", spec, baseline.DeepCopy());
		var rng = rollSeed.HasValue ? new Random(rollSeed.Value) : null;

		while (remaining > 0)
		{
			var legal = CollectLegalMoves(ship, remaining);
			if (legal.Count == 0)
				break;

			var move = rng is null
				? legal.OrderBy(m => m, TierRollMoveCanonicalComparer.Instance).First()
				: legal[rng.Next(legal.Count)];

			if (!move.TryApply(ship, out ship))
				break;

			remaining -= move.Cost;
		}

		return ship.Loadout.DeepCopy();
	}

	private static List<TierRollMove> CollectLegalMoves(ShipInstance ship, int remainingBudget)
	{
		var moves = new List<TierRollMove>();

		foreach (var slot in ship.Spec.Slots)
		{
			if (ship.Loadout.InstalledAbilities.Any(installed => installed.Mount == slot.Mount))
				continue;

			var candidate = new InstalledAbility(slot.Baseline, slot.Mount.Facet);
			if (CostInstallWeapon > remainingBudget)
				continue;
			if (!ship.TryWithInstalledAbility(candidate, out _))
				continue;

			moves.Add(TierRollMove.Install(CostInstallWeapon, candidate));
		}

		if (CostHullMax <= remainingBudget && ship.TryWithUpgradedMaxHull(out _))
			moves.Add(TierRollMove.Hull(CostHullMax));

		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
		{
			if (CostShieldMax > remainingBudget)
				continue;
			if (!ship.TryWithUpgradedMaxShields(face, out _))
				continue;

			moves.Add(TierRollMove.Shield(CostShieldMax, face));
		}

		foreach (var installed in ship.Loadout.InstalledAbilities)
		{
			var mount = installed.Mount;
			if (CostWeaponDamage <= remainingBudget && ship.TryWithDamageUpgraded(mount, out _))
				moves.Add(TierRollMove.Damage(CostWeaponDamage, mount));

			if (CostWeaponRange <= remainingBudget && ship.TryWithRangeUpgraded(mount, out _))
				moves.Add(TierRollMove.Range(CostWeaponRange, mount));
		}

		return moves;
	}

	private enum LoadoutUpgradeKind
	{
		InstallWeapon = 0,
		WeaponDamage = 1,
		HullMax = 2,
		WeaponRange = 3,
		ShieldMax = 4,
	}

	private sealed class TierRollMove
	{
		public int Cost { get; }
		public LoadoutUpgradeKind Kind { get; }
		public InstalledAbility? Installed { get; }
		public ESpatialOrientation? Face { get; }
		public AbilityMount? Mount { get; }

		private TierRollMove(
			int cost,
			LoadoutUpgradeKind kind,
			InstalledAbility? install,
			ESpatialOrientation? face,
			AbilityMount? mount)
		{
			Cost = cost;
			Kind = kind;
			Installed = install;
			Face = face;
			Mount = mount;
		}

		public static TierRollMove Install(int cost, InstalledAbility install) =>
			new(cost, LoadoutUpgradeKind.InstallWeapon, install, null, null);

		public static TierRollMove Hull(int cost) =>
			new(cost, LoadoutUpgradeKind.HullMax, null, null, null);

		public static TierRollMove Shield(int cost, ESpatialOrientation face) =>
			new(cost, LoadoutUpgradeKind.ShieldMax, null, face, null);

		public static TierRollMove Damage(int cost, AbilityMount mount) =>
			new(cost, LoadoutUpgradeKind.WeaponDamage, null, null, mount);

		public static TierRollMove Range(int cost, AbilityMount mount) =>
			new(cost, LoadoutUpgradeKind.WeaponRange, null, null, mount);

		public bool TryApply(ShipInstance ship, out ShipInstance after)
		{
			after = null!;
			return Kind switch
			{
				LoadoutUpgradeKind.InstallWeapon => ship.TryWithInstalledAbility(Installed!, out after),
				LoadoutUpgradeKind.HullMax => ship.TryWithUpgradedMaxHull(out after),
				LoadoutUpgradeKind.ShieldMax => ship.TryWithUpgradedMaxShields(Face!.Value, out after),
				LoadoutUpgradeKind.WeaponDamage => ship.TryWithDamageUpgraded(Mount!.Value, out after),
				LoadoutUpgradeKind.WeaponRange => ship.TryWithRangeUpgraded(Mount!.Value, out after),
				_ => false,
			};
		}
	}

	private sealed class TierRollMoveCanonicalComparer : IComparer<TierRollMove>
	{
		public static TierRollMoveCanonicalComparer Instance { get; } = new();

		public int Compare(TierRollMove? x, TierRollMove? y)
		{
			if (ReferenceEquals(x, y))
				return 0;
			if (x is null)
				return -1;
			if (y is null)
				return 1;

			var kind = ((int)x.Kind).CompareTo((int)y.Kind);
			if (kind != 0)
				return kind;

			return CompareMountOrFace(x, y);
		}

		private static int CompareMountOrFace(TierRollMove x, TierRollMove y)
		{
			var faceX = x.Face ?? x.Installed?.MountedOn;
			var faceY = y.Face ?? y.Installed?.MountedOn;
			if (faceX.HasValue || faceY.HasValue)
			{
				var face = ((int)(faceX ?? 0)).CompareTo((int)(faceY ?? 0));
				if (face != 0)
					return face;
			}

			var mountX = x.Mount ?? x.Installed?.Mount;
			var mountY = y.Mount ?? y.Installed?.Mount;
			if (mountX is null && mountY is null)
				return 0;
			if (mountX is null)
				return -1;
			if (mountY is null)
				return 1;

			var kindOrder = ((int)mountX.Value.Kind).CompareTo((int)mountY.Value.Kind);
			if (kindOrder != 0)
				return kindOrder;

			return ((int)mountX.Value.Facet).CompareTo((int)mountY.Value.Facet);
		}
	}
}
