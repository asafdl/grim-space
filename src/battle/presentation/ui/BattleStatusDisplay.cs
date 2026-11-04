using GrimSpace.Battle.Player;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Components;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed record BattleStatusDisplay(
	string Key,
	string IconName,
	EStatusPolarity Polarity,
	int? StackCount,
	HudTooltipContent Tooltip)
{
	public static IReadOnlyList<BattleStatusDisplay> Capture(UnitDisplayState unit)
	{
		ArgumentNullException.ThrowIfNull(unit);
		if (unit.Statuses is null)
			return [];

		return BattleStatuses.Ordered(unit.Statuses)
			.GroupBy(StatusKey, StringComparer.Ordinal)
			.Select(group => Create(unit, group.Key, group.ToArray()))
			.ToArray();
	}

	private static string StatusKey(BattleStatus status)
	{
		var parts = status.InstanceId.Split(':', 3);
		if (parts.Length != 3 || parts.Any(string.IsNullOrWhiteSpace))
			throw new InvalidDataException($"Malformed status instance id '{status.InstanceId}'.");
		return $"{parts[0]}:{parts[1]}";
	}

	private static BattleStatusDisplay Create(
		UnitDisplayState unit,
		string key,
		IReadOnlyList<BattleStatus> statuses)
	{
		var status = statuses[0];
		var (icon, tooltip) = key switch
		{
			"buff:Overdrive" => ("Overdrive", new HudTooltipContent(
				"Overdrive",
				$"Deal {unit.Loadout.OverdriveDamageBonusPercent}% additional weapon damage this turn.",
				"Overheat follows next turn.")),
			"buff:Overheat" => ("Overheat", new HudTooltipContent(
				"Overheat",
				"All abilities are on cooldown this turn.",
				"Clears next turn.")),
			"buff:ProtectionDrone" => ("ProtectionDrone", BattleHudCopy.ProtectionDroneTooltipFor(ProtectionDroneSpec.Baseline)),
			"buff:MarkWeakspot" when status.Payload is MarkWeakspotPayload mark =>
				("Crit", PilotSkillCatalog.Get(EPilotSkillId.MarkWeakspot).Tooltip with
				{
					Details = $"Marked face: {BattleHudCopy.FaceName(mark.Face)}",
				}),
			"buff:ShieldBrace" when status.Payload is ShieldBracePayload brace =>
				("Shield", PilotSkillCatalog.Get(EPilotSkillId.Brace).Tooltip with
				{
					Details = $"Braced face: {brace.Face}\nShield damage to redistribute: {brace.DamagePending}",
				}),
			"buff:TemporaryShield" => ("Shield", TemporaryShieldTooltip(statuses)),
			"passive:ScrapDroneSwarmDamageBonus" => ("ScrapDroneSwarm", new HudTooltipContent(
				"Scrap Drone",
				$"Scrap Drone Swarm deals {PilotSkillPercents.ScrapDrone.SwarmAdditionalDamage} additional damage.")),
			"passive:HealingSkillRangeBonus" => ("Heal", PilotSkillCatalog.Get(EPilotSkillId.HelpABotterOut).Tooltip),
			"passive:LightningShieldDamageBonus" => ("LightningCannon", PilotSkillCatalog.Get(EPilotSkillId.IonizedPayload).Tooltip),
			"passive:AdjacentDamageBonus" => ("Damage", PilotSkillCatalog.Get(EPilotSkillId.DodgeThis).Tooltip),
			"passive:ReinforcedHull" => ("Hull", PilotSkillCatalog.Get(EPilotSkillId.ReinforcedHull).Tooltip),
			"passive:InTheFray" => ("Shield", PilotSkillCatalog.Get(EPilotSkillId.InTheFray).Tooltip),
			"passive:ReactiveShields" => ("Shield", PilotSkillCatalog.Get(EPilotSkillId.ReactiveShields).Tooltip with
			{
				Details = $"Stacks: {unit.ReactiveShieldsStacks}\nReductions stack multiplicatively. Resets at the start of your turn.",
			}),
			"passive:StationaryDamageDeflection" => ("CurrentManeuverPoints", PilotSkillCatalog.Get(EPilotSkillId.DefensiveManeuvers).Tooltip),
			"passive:ShieldDynamo" => ("Shield", PilotSkillCatalog.Get(EPilotSkillId.ShieldDynamo).Tooltip),
			"passive:ThirstForScrap" => ("Damage", PilotSkillCatalog.Get(EPilotSkillId.ThirstForScrap).Tooltip with
			{
				Details = $"Stacks: {unit.ThirstForScrapStacks}\nCurrent damage bonus: +{unit.ThirstForScrapStacks * PilotSkillPercents.ThirstForScrap.DamageBonusPercentPerKill}%. Lasts for this battle.",
			}),
			"passive:YouLittleRat" => ("Shield", PilotSkillCatalog.Get(EPilotSkillId.YouLittleRat).Tooltip),
			"passive:OneWithTheVoid" => ("VoidBombLauncher", PilotSkillCatalog.Get(EPilotSkillId.OneWithTheVoid).Tooltip),
			_ => throw new InvalidDataException($"Unknown battle status display '{status.InstanceId}'."),
		};
		int? stacks = key switch
		{
			"passive:ReactiveShields" => unit.ReactiveShieldsStacks,
			"passive:ThirstForScrap" => unit.ThirstForScrapStacks,
			"buff:TemporaryShield" => statuses.Count,
			_ => null,
		};
		return new BattleStatusDisplay(key, icon, status.Polarity, stacks, tooltip);
	}

	private static HudTooltipContent TemporaryShieldTooltip(IReadOnlyList<BattleStatus> statuses)
	{
		var payloads = statuses.Select(status => status.Payload is TemporaryShieldPayload payload
			? payload
			: throw new InvalidDataException($"Temporary shield status '{status.InstanceId}' has no shield payload."))
			.ToArray();
		var faces = Enum.GetValues<ESpatialOrientation>()
			.Select(face => $"{face}: {payloads.Sum(payload => payload.Remaining[face])}");
		return new HudTooltipContent(
			"Temporary Shields",
			"Extra shields absorb damage before regular shields. Grants stack and expire independently.",
			$"Grants: {statuses.Count}\nRemaining temporary shields:\n{string.Join("\n", faces)}");
	}
}
