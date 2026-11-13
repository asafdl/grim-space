using GrimSpace.Components;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Merchants;
using RunState = GrimSpace.Run.State;
using BattleUnitState = GrimSpace.Battle.Units.State;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

internal enum ShipStatKind
{
	Hull,
	Shield,
	HeatCapacity,
	HeatRecovery,
	ActionPoints,
	ManeuverPoints,
	Initiative,
	WeaponDamageBonus,
	OverdriveDamageBonus,
}

internal readonly record struct ShipStat(
	ShipStatKind Kind,
	int Value,
	int? Delta,
	ESpatialOrientation? Face = null);

internal static class ShipRecruitmentDisplay
{
	public static IReadOnlyList<ShipStat> Stats(ShipInstance ship, ShipInstance? baseline = null)
	{
		ArgumentNullException.ThrowIfNull(ship);
		var current = BattleUnitState.FromShipInstance(ship, Coord.Zero);
		var previous = baseline is null ? null : BattleUnitState.FromShipInstance(baseline, Coord.Zero);
		var stats = new List<ShipStat>();

		void Add(ShipStatKind kind, int value, int? old, ESpatialOrientation? face = null) =>
			stats.Add(new ShipStat(kind, value, old.HasValue ? value - old.Value : null, face));

		Add(ShipStatKind.Hull, current.Loadout.BaseHullPoints, previous?.Loadout.BaseHullPoints);
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			Add(ShipStatKind.Shield, current.Loadout.BaseShieldPoints[face], previous?.Loadout.BaseShieldPoints[face], face);
		Add(ShipStatKind.HeatCapacity, current.HeatProfile.EffectiveCapacity, previous?.HeatProfile.EffectiveCapacity);
		Add(ShipStatKind.HeatRecovery, current.HeatProfile.ClearPerTurn, previous?.HeatProfile.ClearPerTurn);
		Add(ShipStatKind.ActionPoints, current.Maneuverability.BaseActionPoints, previous?.Maneuverability.BaseActionPoints);
		Add(ShipStatKind.ManeuverPoints, current.Maneuverability.BaseManeuverPoints, previous?.Maneuverability.BaseManeuverPoints);
		Add(ShipStatKind.Initiative, current.Initiative, previous?.Initiative);
		Add(ShipStatKind.WeaponDamageBonus, current.Loadout.WeaponDamagePercent, previous?.Loadout.WeaponDamagePercent);
		Add(ShipStatKind.OverdriveDamageBonus, current.Loadout.OverdriveDamageBonusPercent, previous?.Loadout.OverdriveDamageBonusPercent);
		return stats;
	}

	public static ShipInstance? BrokerBaseline(RunState run, EType chassis, Random random)
	{
		ArgumentNullException.ThrowIfNull(run);
		ArgumentNullException.ThrowIfNull(random);
		var matches = run.GetFieldedShipIds()
			.Select(run.ItemInventory.Get)
			.Where(ship => ship.Spec.Chassis == chassis)
			.Select(ship => (Ship: ship, Tier: ShipLoadoutGearTier.FromLoadout(ship.Loadout, chassis)))
			.ToArray();
		if (matches.Length == 0)
			return null;
		var highest = matches.Max(match => match.Tier);
		var best = matches.Where(match => match.Tier == highest).ToArray();
		return best[random.Next(best.Length)].Ship;
	}

	public static HudTooltipContent OfferTooltip(RecruitmentOffer offer, ShipInstance? baseline = null)
	{
		ArgumentNullException.ThrowIfNull(offer);
		var ship = ShipInstance.FromSpec(
			"ship-offer",
			ShipCatalog.SpecFor(offer.Chassis),
			ShipCatalog.LoadoutForTier(offer.Chassis, offer.GearTier, offer.RollSeed));
		return BuildTooltip(ship, offer.GearTier, baseline);
	}

	public static HudTooltipContent ShipTooltip(ShipInstance ship) =>
		BuildTooltip(ship, ShipLoadoutGearTier.FromLoadout(ship.Loadout, ship.Spec.Chassis), null);

	public static HudTooltipContent CompareTooltip(ShipInstance candidate, ShipInstance? equipped) =>
		BuildTooltip(candidate, ShipLoadoutGearTier.FromLoadout(candidate.Loadout, candidate.Spec.Chassis), equipped);

	private static HudTooltipContent BuildTooltip(ShipInstance ship, EShipGearTier tier, ShipInstance? baseline)
	{
		var title = $"{PilotHireDisplay.ShipChassisLabel(ship.Spec.Chassis)} {tier}";
		var tooltip = StatTooltip(title, ship);
		if (baseline is null)
			return tooltip;

		var currentTitle = $"{PilotHireDisplay.ShipChassisLabel(baseline.Spec.Chassis)} "
			+ ShipLoadoutGearTier.FromLoadout(baseline.Loadout, baseline.Spec.Chassis);
		return tooltip with
		{
			Comparison = new HudTooltipComparison(
				StatTooltip($"Current: {currentTitle}", baseline),
				StatTooltip($"New: {title}", ship, baseline)),
		};
	}

	private static HudTooltipContent StatTooltip(string title, ShipInstance ship, ShipInstance? baseline = null)
	{
		var stats = Stats(ship, baseline);
		return new HudTooltipContent(title, "")
		{
			InlineRows =
			[
				new HudTooltipInlineRow("Shield", stats.Where(stat => stat.Kind == ShipStatKind.Shield).Select(Row).ToArray()),
			],
			Rows = stats.Where(stat => stat.Kind != ShipStatKind.Shield).Select(Row).ToArray(),
			TwoColumnRows = true,
		};
	}

	private static HudTooltipRow Row(ShipStat stat)
	{
		var (label, icon, percent) = stat.Kind switch
		{
			ShipStatKind.Hull => ("Hull", "Hull", false),
			ShipStatKind.Shield => (ShortFace(stat.Face!.Value), null, false),
			ShipStatKind.HeatCapacity => ("Heat", "Heat", false),
			ShipStatKind.HeatRecovery => ("Cooling", "Heat", false),
			ShipStatKind.ActionPoints => ("AP", "CurrentActionPoints", false),
			ShipStatKind.ManeuverPoints => ("MP", "CurrentManeuverPoints", false),
			ShipStatKind.Initiative => ("Initiative", null, false),
			ShipStatKind.WeaponDamageBonus => ("Damage", "Damage", true),
			ShipStatKind.OverdriveDamageBonus => ("Overdrive", "Overdrive", true),
			_ => throw new ArgumentOutOfRangeException(nameof(stat), stat.Kind, null),
		};
		var suffix = percent ? "%" : "";
		return new HudTooltipRow(label, $"{stat.Value}{suffix}", icon, stat.Delta, suffix);
	}

	private static string ShortFace(ESpatialOrientation face) =>
		face switch
		{
			ESpatialOrientation.Forward => "F",
			ESpatialOrientation.Retro => "A",
			ESpatialOrientation.Port => "P",
			ESpatialOrientation.Starboard => "S",
			ESpatialOrientation.Dorsal => "D",
			ESpatialOrientation.Ventral => "V",
			_ => throw new ArgumentOutOfRangeException(nameof(face), face, null),
		};
}
