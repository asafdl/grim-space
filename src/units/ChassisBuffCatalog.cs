using GrimSpace.Math.Grid;
using GrimSpace.Units.Heat;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;

namespace GrimSpace.Units;

public enum EChassisBuffOffer
{
	ShieldFlat,
	ShieldPercent,
	HullPercent,
	HeatCapacity,
	HeatRecovery,
	OverdriveDamage,
	WeaponDamage,
	ShieldFlatHeavy,
	ShieldPercentHeavy,
	HullPercentHeavy,
	HeatCapacityHeavy,
	HeatRecoveryHeavy,
	OverdriveDamageHeavy,
	WeaponDamageHeavy,
	WeaponRange,
	ManeuverPoint,
	ActionPoint,
	WeaponRangeHeat,
	ManeuverPointHeat,
	ActionPointHeat,
}

internal readonly record struct ChassisBuffTotals(
	int ShieldFlat,
	int ShieldPercent,
	int HullPercent,
	int HeatCapacityPercent,
	int HeatRecoveryPercent,
	int OverdrivePoints,
	int WeaponDamagePercent,
	int RangeSteps,
	int CurrentActionPoints,
	int CurrentManeuverPoints);

internal static class ChassisBuffCatalog
{
	public const int StandardNetCost = 2;
	public const int StrongNetCost = 4;
	public static readonly EChassisBuffOffer[] All = Enum.GetValues<EChassisBuffOffer>();

	public static int NetCostOf(EChassisBuffOffer offer) =>
		offer is EChassisBuffOffer.WeaponRange
			or EChassisBuffOffer.ManeuverPoint
			or EChassisBuffOffer.ActionPoint
			? StrongNetCost
			: StandardNetCost;

	public static bool IsOnce(EChassisBuffOffer offer) =>
		offer is EChassisBuffOffer.WeaponRange
			or EChassisBuffOffer.ManeuverPoint
			or EChassisBuffOffer.ActionPoint
			or EChassisBuffOffer.WeaponRangeHeat
			or EChassisBuffOffer.ManeuverPointHeat
			or EChassisBuffOffer.ActionPointHeat;

	public static bool TouchesHeat(EChassisBuffOffer offer) =>
		offer is EChassisBuffOffer.HeatCapacity
			or EChassisBuffOffer.HeatRecovery
			or EChassisBuffOffer.OverdriveDamage
			or EChassisBuffOffer.ShieldFlatHeavy
			or EChassisBuffOffer.ShieldPercentHeavy
			or EChassisBuffOffer.HullPercentHeavy
			or EChassisBuffOffer.HeatCapacityHeavy
			or EChassisBuffOffer.HeatRecoveryHeavy
			or EChassisBuffOffer.OverdriveDamageHeavy
			or EChassisBuffOffer.WeaponDamageHeavy
			or EChassisBuffOffer.WeaponRangeHeat
			or EChassisBuffOffer.ManeuverPointHeat
			or EChassisBuffOffer.ActionPointHeat;

	public static bool GrantsRange(EChassisBuffOffer offer) =>
		offer is EChassisBuffOffer.WeaponRange or EChassisBuffOffer.WeaponRangeHeat;

	public static bool IsAvailable(
		EChassisBuffOffer offer,
		ShipSpec spec,
		ShipLoadout loadout,
		IReadOnlySet<EChassisBuffOffer> takenOnce,
		int rangeStepsTaken)
	{
		ArgumentNullException.ThrowIfNull(spec);
		ArgumentNullException.ThrowIfNull(loadout);
		ArgumentNullException.ThrowIfNull(takenOnce);
		if (IsOnce(offer) && takenOnce.Contains(offer))
			return false;
		if (TouchesHeat(offer) && !spec.Heat.Participates)
			return false;
		if (GrantsRange(offer) && !HasRangeRoom(loadout, rangeStepsTaken))
			return false;
		return true;
	}

	public static ChassisBuffTotals Sum(IReadOnlyList<EChassisBuffOffer>? offers)
	{
		var shieldFlat = 0;
		var shieldPercent = 0;
		var hullPercent = 0;
		var heatCapacityPercent = 0;
		var heatRecoveryPercent = 0;
		var overdrivePoints = 0;
		var weaponDamagePercent = 0;
		var rangeSteps = 0;
		var actionPoints = 0;
		var maneuverPoints = 0;
		if (offers is null)
			return new ChassisBuffTotals();

		foreach (var offer in offers)
		{
			switch (offer)
			{
				case EChassisBuffOffer.ShieldFlat:
					shieldFlat += 30;
					break;
				case EChassisBuffOffer.ShieldPercent:
					shieldPercent += 15;
					break;
				case EChassisBuffOffer.HullPercent:
					hullPercent += 15;
					break;
				case EChassisBuffOffer.HeatCapacity:
					heatCapacityPercent += 20;
					break;
				case EChassisBuffOffer.HeatRecovery:
					heatRecoveryPercent += 20;
					break;
				case EChassisBuffOffer.OverdriveDamage:
					overdrivePoints += 20;
					break;
				case EChassisBuffOffer.WeaponDamage:
					weaponDamagePercent += 20;
					break;
				case EChassisBuffOffer.ShieldFlatHeavy:
					shieldFlat += 80;
					heatCapacityPercent -= 15;
					break;
				case EChassisBuffOffer.ShieldPercentHeavy:
					shieldPercent += 40;
					heatRecoveryPercent -= 15;
					break;
				case EChassisBuffOffer.HullPercentHeavy:
					hullPercent += 40;
					heatCapacityPercent -= 15;
					break;
				case EChassisBuffOffer.HeatCapacityHeavy:
					heatCapacityPercent += 50;
					shieldPercent -= 15;
					break;
				case EChassisBuffOffer.HeatRecoveryHeavy:
					heatRecoveryPercent += 50;
					hullPercent -= 15;
					break;
				case EChassisBuffOffer.OverdriveDamageHeavy:
					overdrivePoints += 50;
					weaponDamagePercent -= 15;
					break;
				case EChassisBuffOffer.WeaponDamageHeavy:
					weaponDamagePercent += 50;
					heatCapacityPercent -= 15;
					break;
				case EChassisBuffOffer.WeaponRange:
					rangeSteps += 1;
					break;
				case EChassisBuffOffer.ManeuverPoint:
					maneuverPoints += 1;
					break;
				case EChassisBuffOffer.ActionPoint:
					actionPoints += 1;
					break;
				case EChassisBuffOffer.WeaponRangeHeat:
					rangeSteps += 1;
					heatCapacityPercent -= 10;
					break;
				case EChassisBuffOffer.ManeuverPointHeat:
					maneuverPoints += 1;
					heatCapacityPercent -= 10;
					break;
				case EChassisBuffOffer.ActionPointHeat:
					actionPoints += 1;
					heatCapacityPercent -= 10;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(offers), offer, null);
			}
		}

		return new ChassisBuffTotals(
			shieldFlat,
			shieldPercent,
			hullPercent,
			heatCapacityPercent,
			heatRecoveryPercent,
			overdrivePoints,
			weaponDamagePercent,
			rangeSteps,
			actionPoints,
			maneuverPoints);
	}

	public static (int WeaponDamagePercent, int OverdriveDamageBonusPercent) DamageBonuses(
		IReadOnlyList<EChassisBuffOffer>? offers)
	{
		var totals = Sum(offers);
		return (
			totals.WeaponDamagePercent,
			ShipLoadout.BaselineOverdriveDamageBonusPercent + totals.OverdrivePoints);
	}

	public static ShipLoadout Apply(
		ShipSpec spec,
		ShipLoadout baseline,
		IReadOnlyList<EChassisBuffOffer> offers)
	{
		ArgumentNullException.ThrowIfNull(spec);
		ArgumentNullException.ThrowIfNull(baseline);
		ArgumentNullException.ThrowIfNull(offers);
		var totals = Sum(offers);
		var shields = baseline.BaseShieldPoints.Clone();
		var shieldScale = 100 + totals.ShieldPercent;
		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
			shields[face] = System.Math.Max(0, Scale(shields[face] + totals.ShieldFlat, shieldScale));

		var hull = System.Math.Max(0, Scale(baseline.BaseHullPoints, 100 + totals.HullPercent));
		return ShipLoadout.Create(
			spec,
			hull,
			shields,
			ApplyRange(baseline.InstalledAbilities, totals.RangeSteps),
			baseline.ShieldUpgradeTiers,
			baseline.HullUpgradeTier,
			offers);
	}

	public static HeatSpec ScaleHeat(HeatSpec baseline, ChassisBuffTotals totals)
	{
		ArgumentNullException.ThrowIfNull(baseline);
		if (!baseline.Participates)
			return baseline;

		return new HeatSpec(
			System.Math.Max(0, Scale(baseline.EffectiveCapacity, 100 + totals.HeatCapacityPercent)),
			System.Math.Max(0, Scale(baseline.ClearPerTurn, 100 + totals.HeatRecoveryPercent)),
			baseline.GainMultiplier);
	}

	public static int Scale(int amount, int percent) =>
		(int)System.Math.Round(amount * percent / 100m, MidpointRounding.AwayFromZero);

	private static bool HasRangeRoom(ShipLoadout loadout, int stepsTaken)
	{
		foreach (var ability in loadout.InstalledAbilities)
		{
			if (ability.RangeUpgradeTier + stepsTaken < ability.Spec.MaxRangeUpgrades)
				return true;
		}

		return false;
	}

	private static IReadOnlyList<InstalledAbility> ApplyRange(
		IReadOnlyList<InstalledAbility> abilities,
		int steps)
	{
		if (steps <= 0)
			return abilities.ToArray();

		return abilities.Select(ability =>
		{
			var room = ability.Spec.MaxRangeUpgrades - ability.RangeUpgradeTier;
			var grant = System.Math.Min(steps, System.Math.Max(0, room));
			return grant == 0
				? ability
				: ability.WithRangeUpgradeTier(ability.RangeUpgradeTier + grant);
		}).ToArray();
	}
}
