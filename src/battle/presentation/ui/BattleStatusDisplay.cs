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

		var displays = new List<BattleStatusDisplay>();
		if (unit.Overdrive)
			displays.Add(new BattleStatusDisplay(
				"Overdrive",
				"Overdrive",
				EStatusPolarity.Buff,
				null,
				new HudTooltipContent(
					"Overdrive",
					$"Deal {unit.Loadout.OverdriveDamageBonusPercent}% additional weapon damage this turn.",
					"Overheat follows next turn.")));
		if (unit.Overheat)
			displays.Add(new BattleStatusDisplay(
				"Overheat",
				"Overheat",
				EStatusPolarity.Debuff,
				null,
				new HudTooltipContent(
					"Overheat",
					"All abilities are on cooldown this turn.",
					"Clears next turn.")));

		foreach (var group in unit.Statuses.GroupBy(status => status.GetType()))
		{
			var status = group.First();
			displays.Add(Create(unit, status, group.ToArray()));
		}

		return displays;
	}

	private static BattleStatusDisplay Create(
		UnitDisplayState unit,
		BattleStatus status,
		IReadOnlyList<BattleStatus> statuses) =>
		status switch
		{
			ProtectionDroneBuff => new(
				nameof(ProtectionDroneBuff),
				"ProtectionDrone",
				status.Polarity,
				null,
				BattleHudCopy.ProtectionDroneTooltipFor(ProtectionDroneSpec.Baseline)),
			MarkWeakspotDebuff mark => new(
				nameof(MarkWeakspotDebuff),
				"Crit",
				status.Polarity,
				null,
				PilotSkillCatalog.Get(EPilotSkillId.MarkWeakspot).Tooltip with
				{
					Details = $"Marked face: {BattleHudCopy.FaceName(mark.Face)}",
				}),
			ShieldBraceStatus brace => new(
				nameof(ShieldBraceStatus),
				"Shield",
				status.Polarity,
				null,
				PilotSkillCatalog.Get(EPilotSkillId.Brace).Tooltip with
				{
					Details = $"Braced face: {brace.Face}\nShield damage to redistribute: {brace.DamagePending}",
				}),
			TemporaryShieldGrantStatus => new(
				nameof(TemporaryShieldGrantStatus),
				"Shield",
				status.Polarity,
				statuses.Count,
				TemporaryShieldTooltip(statuses.OfType<TemporaryShieldGrantStatus>().ToArray())),
			ScrapDroneSwarmDamageBonus => Display(
				status, "ScrapDroneSwarm", new HudTooltipContent(
					"Scrap Drone",
					$"Scrap Drone Swarm deals {PilotSkillPercents.ScrapDrone.SwarmDamageBonusPercent}% additional damage.")),
			HealingSkillRangeBonus => Display(
				status, "Heal", PilotSkillCatalog.Get(EPilotSkillId.HelpABotterOut).Tooltip),
			LightningShieldDamageBonus => Display(
				status, "LightningCannon", PilotSkillCatalog.Get(EPilotSkillId.IonizedPayload).Tooltip),
			AdjacentDamageBonus => Display(
				status, "Damage", PilotSkillCatalog.Get(EPilotSkillId.DodgeThis).Tooltip),
			ReinforcedHull => Display(
				status, "Hull", PilotSkillCatalog.Get(EPilotSkillId.ReinforcedHull).Tooltip),
			InTheFray => Display(
				status, "Shield", PilotSkillCatalog.Get(EPilotSkillId.InTheFray).Tooltip),
			ReactiveShields reactive => new(
				nameof(ReactiveShields),
				"Shield",
				status.Polarity,
				reactive.Stacks,
				PilotSkillCatalog.Get(EPilotSkillId.ReactiveShields).Tooltip with
				{
					Details = $"Stacks: {reactive.Stacks}\nReductions stack multiplicatively. Resets at the start of your turn.",
				}),
			StationaryDamageDeflection => Display(
				status, "CurrentManeuverPoints", PilotSkillCatalog.Get(EPilotSkillId.DefensiveManeuvers).Tooltip),
			ShieldDynamo => Display(
				status, "Shield", PilotSkillCatalog.Get(EPilotSkillId.ShieldDynamo).Tooltip),
			ThirstForScrap thirst => new(
				nameof(ThirstForScrap),
				"Damage",
				status.Polarity,
				thirst.Stacks,
				PilotSkillCatalog.Get(EPilotSkillId.ThirstForScrap).Tooltip with
				{
					Details = $"Stacks: {thirst.Stacks}\nCurrent damage bonus: +{thirst.Stacks * PilotSkillPercents.ThirstForScrap.DamageBonusPercentPerKill}%. Lasts for this battle.",
				}),
			YouLittleRat => Display(
				status, "Shield", PilotSkillCatalog.Get(EPilotSkillId.YouLittleRat).Tooltip),
			NothingButMetal => Display(
				status, "Damage", PilotSkillCatalog.Get(EPilotSkillId.NothingButMetal).Tooltip),
			SpawnedUnitDamageBonus => Display(
				status, "Damage", PilotSkillCatalog.Get(EPilotSkillId.NotFullyFormed).Tooltip),
			OneWithTheVoid => Display(
				status, "VoidBombLauncher", PilotSkillCatalog.Get(EPilotSkillId.OneWithTheVoid).Tooltip),
			OutgoingDamageBonus => Display(
				status, "Damage", new HudTooltipContent(
					"Asteroid Surfer",
					$"[icon: Damage, text: +{PilotSkillPercents.AsteroidSurfer.OutgoingDamageBonusPercent}%] damage dealt.")),
			PirateDamageBonus => Display(
				status, "Damage", PilotQuirkCatalog.Get(EPilotQuirkId.PirateHater).Tooltip),
			HealBonus => Display(
				status, "Heal", PilotQuirkCatalog.Get(EPilotQuirkId.ICanFixThat).Tooltip),
			HealPenalty => Display(
				status, "Heal", PilotQuirkCatalog.Get(EPilotQuirkId.RoughPatchJob).Tooltip),
			IncreasedHullDamageTaken => Display(
				status, "Hull", new HudTooltipContent(
					"Asteroid Surfer",
					$"[icon: Hull, text: +{PilotSkillPercents.AsteroidSurfer.HullDamageTakenPercent - 100}%] hull damage taken.")),
			SpawnedAllyDamageBonus => Display(
				status, "Damage", new HudTooltipContent(
					"Retired Machine Manager",
					$"Spawned units get [icon: Damage, text: +{PilotSkillPercents.RetiredMachineManager.SpawnedAllyDamageBonusPercent}%] damage.")),
			SpawnedAllyDamageBuff => Display(
				status, "Damage", new HudTooltipContent(
					"Spawn Damage",
					$"[icon: Damage, text: +{PilotSkillPercents.RetiredMachineManager.SpawnedAllyDamageBonusPercent}%] damage.")),
			DamageResistance => Display(
				status, "Hull", new HudTooltipContent(
					"Hydraulic Abuser",
					$"[icon: Hull, text: +{100 - PilotSkillPercents.HydraulicAbuser.DamageTakenPercent}%] damage resistance.")),
			_ => throw new InvalidDataException($"Unknown battle status display '{status.GetType().Name}'."),
		};

	private static BattleStatusDisplay Display(BattleStatus status, string icon, HudTooltipContent tooltip) =>
		new(status.GetType().Name, icon, status.Polarity, null, tooltip);

	private static HudTooltipContent TemporaryShieldTooltip(IReadOnlyList<TemporaryShieldGrantStatus> grants)
	{
		var faces = Enum.GetValues<ESpatialOrientation>()
			.Select(face => $"{BattleHudCopy.FaceAbbrev(face)}: {grants.Sum(grant => grant.Remaining[face])}");
		return new HudTooltipContent(
			"Temporary Shields",
			"Extra shields absorb damage before regular shields.",
			$"{string.Join(" ", faces)}");
	}
}
