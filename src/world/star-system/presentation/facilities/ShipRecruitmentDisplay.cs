using System.Text;
using GrimSpace.Math.Grid;
using GrimSpace.Components;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Battle.Presentation;
using GrimSpace.Units.Loadouts;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

internal static class ShipRecruitmentDisplay
{
	public static HudTooltipContent OfferTooltip(RecruitmentOffer offer)
	{
		ArgumentNullException.ThrowIfNull(offer);

		var title = $"{PilotHireDisplay.ShipChassisLabel(offer.Chassis)} {offer.GearTier}";
		var stock = ShipCatalog.LoadoutForTier(offer.Chassis, EShipGearTier.T0);
		var candidate = ShipCatalog.LoadoutForTier(offer.Chassis, offer.GearTier, offer.RollSeed);
		var changes = new List<(bool Improves, string Line)>();
		AddHardwareDiff(changes, ShipHardwareDiff.Compare(offer.Chassis, stock, offer.Chassis, candidate));
		AddMountDeltas(changes, stock, candidate);
		AddBuffTotals(changes, ChassisBuffCatalog.Sum(candidate.Buffs));
		var body = string.Join(
			HudTooltipContent.Divider,
			new[] { BaseStats(stock), Lines(changes, improves: true), Lines(changes, improves: false) }
				.Where(section => section.Length > 0));
		return new HudTooltipContent(title, body);
	}

	private static string Lines(IEnumerable<(bool Improves, string Line)> changes, bool improves) =>
		string.Join('\n', changes.Where(change => change.Improves == improves).Select(change => change.Line));

	private static string BaseStats(ShipLoadout loadout)
	{
		var text = new StringBuilder($"[icon: Hull, text: {loadout.MaxHullPoints}]");
		var shieldGroups = Enum.GetValues<ESpatialOrientation>()
			.Where(face => loadout.MaxShieldPoints[face] > 0)
			.GroupBy(face => loadout.MaxShieldPoints[face])
			.ToArray();
		foreach (var group in shieldGroups)
		{
			text.Append($"  [icon: Shield, text: {group.Key}");
			if (group.Count() < Enum.GetValues<ESpatialOrientation>().Length)
				text.Append(' ').Append(string.Join('/', group.Select(ShortFace)));
			text.Append(']');
		}

		foreach (var ability in loadout.InstalledAbilities.DistinctBy(installed => installed.Spec))
		{
			text.Append($"\n[icon: {ability.Kind}]");
			foreach (var stat in AbilityStats(ability.Spec))
				text.Append($" [icon: {stat.Icon}, text: {FormatStat(stat, stat.Value, signed: false)}]");
		}

		return text.ToString();
	}

	private static void AddHardwareDiff(List<(bool Improves, string Line)> changes, ShipHardwareDiff diff)
	{
		if (diff.MaxHullDelta != 0)
			AddStat(changes, "Hull", diff.MaxHullDelta);

		if (diff.ShieldDeltas.Count == Enum.GetValues<ESpatialOrientation>().Length
			&& diff.ShieldDeltas.All(shield => shield.Delta == diff.ShieldDeltas[0].Delta))
			AddStat(changes, "Shield", diff.ShieldDeltas[0].Delta);
		else
			foreach (var (face, delta) in diff.ShieldDeltas)
				AddStat(changes, "Shield", delta, $" {ShortFace(face)}");

		foreach (var ability in diff.AddedAbilities)
			changes.Add((true, $"+ [icon: {ability.Kind}] {ShortFace(ability.MountedOn)}"));
		foreach (var ability in diff.RemovedAbilities)
			changes.Add((false, $"- [icon: {ability.Kind}] {ShortFace(ability.MountedOn)}"));
	}

	private static void AddMountDeltas(List<(bool Improves, string Line)> changes, ShipLoadout stock, ShipLoadout candidate)
	{
		var stockByMount = stock.InstalledAbilities.ToDictionary(installed => installed.Mount);
		foreach (var ability in candidate.InstalledAbilities)
		{
			if (!stockByMount.TryGetValue(ability.Mount, out var baseline))
				continue;

			var deltas = AbilityStats(ability.Spec)
				.Zip(AbilityStats(baseline.Spec), (now, was) => (Stat: now, Delta: now.Value - was.Value))
				.Where(change => change.Delta != 0)
				.GroupBy(change => change.Delta > 0 != change.Stat.LowerIsBetter);
			foreach (var group in deltas)
				changes.Add((
					group.Key,
					$"[icon: {ability.Kind}]" + string.Concat(group.Select(change =>
						$" [icon: {change.Stat.Icon}, text: {FormatStat(change.Stat, change.Delta, signed: true)}]"))));
		}
	}

	private static void AddBuffTotals(List<(bool Improves, string Line)> changes, ChassisBuffTotals totals)
	{
		AddStat(changes, "Damage", totals.WeaponDamagePercent, "%");
		AddStat(changes, "Overdrive", totals.OverdrivePoints, "%");
		AddStat(changes, "Heat", totals.HeatCapacityPercent, "%");
		AddStat(changes, "Heat", totals.HeatRecoveryPercent, "%", " Recovery ");
		AddStat(changes, "ActionPoints", totals.ActionPoints);
		AddStat(changes, "ManeuverPoints", totals.ManeuverPoints);
	}

	private static void AddStat(
		List<(bool Improves, string Line)> changes,
		string icon,
		int delta,
		string suffix = "",
		string prefix = "")
	{
		if (delta != 0)
			changes.Add((delta > 0, $"[icon: {icon}, text: {prefix}{Signed(delta)}{suffix}]"));
	}

	private readonly record struct AbilityStat(string Icon, int Value, bool IsDistance = false)
	{
		public bool LowerIsBetter => Icon == "Cooldown";
	}

	private static IReadOnlyList<AbilityStat> AbilityStats(AbilitySpec spec) =>
		spec switch
		{
			ScrapDroneSwarmSpec swarm =>
				[new("Damage", swarm.Damage), new("Heal", swarm.Heal), new("Range", swarm.BurstRange + 1, IsDistance: true)],
			LightningCannonSpec cannon =>
				[new("Damage", cannon.Damage), new("Range", cannon.LineLength, IsDistance: true)],
			VoidBombLauncherSpec launcher =>
				[new("Damage", launcher.BlastDamage), new("Range", launcher.BlastRadius, IsDistance: true), new("Cooldown", launcher.CooldownTurns)],
			GoopGunSpec goop =>
				[new("Range", goop.Range, IsDistance: true), new("Cooldown", goop.UnavailableTurns)],
			MinerBaySpec bay =>
				[new("Cooldown", bay.CooldownTurns)],
			RamSpec ram =>
				[new("Damage", ram.Damage), new("Range", ram.Range, IsDistance: true)],
			HighSpeedRodSpec rod =>
				[new("Damage", rod.Damage), new("Range", rod.Range, IsDistance: true)],
			RoadsWeDontNeedRoadsSpec jump =>
				[new("Range", jump.Range, IsDistance: true), new("Cooldown", jump.CooldownTurns)],
			_ => throw new ArgumentOutOfRangeException(nameof(spec), spec.Kind, null),
		};

	private static string FormatStat(AbilityStat stat, int value, bool signed)
	{
		var text = stat.IsDistance ? BattleDistance.ApproximateCells(value) : value.ToString();
		return signed && value > 0 ? $"+{text}" : text;
	}

	private static string ShortFace(ESpatialOrientation face) =>
		face switch
		{
			ESpatialOrientation.Forward => "Fwd",
			ESpatialOrientation.Retro => "Aft",
			ESpatialOrientation.Port => "Port",
			ESpatialOrientation.Starboard => "Stbd",
			ESpatialOrientation.Dorsal => "Dors",
			ESpatialOrientation.Ventral => "Vent",
			_ => throw new ArgumentOutOfRangeException(nameof(face), face, null),
		};

	private static string Signed(int value) =>
		value > 0 ? $"+{value}" : value.ToString();
}
