using System.Text;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

internal static class ShipRecruitmentDisplay
{
	public static string ListMetadata(RecruitmentOffer offer) =>
		$"{SalesPitch(offer.Chassis)} · {offer.GearTier}";

	public static string SalesPitch(EType chassis) =>
		chassis switch
		{
			EType.RepurposedMiner =>
				"The workhorse the Syndicate relied on for centuries.",
			EType.Gunship =>
				"A bit hard to manuever, but packs a punch. Great gun support.",
			_ => "Fleet-ready hull from the broker's current manifest.",
		};

	public static string CompareTooltip(RecruitmentOffer offer, ShipInstance? baseline)
	{
		ArgumentNullException.ThrowIfNull(offer);

		if (baseline is null)
		{
			return $"{PilotHireDisplay.ShipPackageLine(offer.Chassis, offer.GearTier)}\n"
				+ "No current ship to compare";
		}

		var candidate = ShipCatalog.LoadoutForTier(offer.Chassis, offer.GearTier);
		var diff = ShipHardwareDiff.Compare(
			baseline.Spec.Chassis,
			baseline.Loadout,
			offer.Chassis,
			candidate);

		var text = new StringBuilder();
		text.Append("vs ")
			.Append(PilotHireDisplay.ShipPackageLine(diff.BaselineChassis, diff.BaselineGearTier));

		if (diff.IsEmpty)
		{
			text.Append("\nNo hardware changes");
			return text.ToString();
		}

		if (diff.ChassisChanged)
			text.Append('\n').Append("Chassis → ").Append(PilotHireDisplay.ShipChassisLabel(diff.CandidateChassis));
		if (diff.GearTierChanged)
			text.Append('\n').Append("Gear tier ").Append(Signed(diff.CandidateGearTier - diff.BaselineGearTier));

		if (diff.MaxHullDelta != 0)
			text.Append('\n').Append("Hull ").Append(Signed(diff.MaxHullDelta));

		foreach (var (face, delta) in diff.ShieldDeltas)
			text.Append('\n')
				.Append(MerchantOfferDisplay.FaceLabel(face))
				.Append(" shield ")
				.Append(Signed(delta));

		if (diff.InitiativeDelta != 0)
			text.Append('\n').Append("Initiative ").Append(Signed(diff.InitiativeDelta));
		if (diff.ActionPointsDelta != 0)
			text.Append('\n').Append("Action points ").Append(Signed(diff.ActionPointsDelta));
		if (diff.ManeuverPointsDelta != 0)
			text.Append('\n').Append("Maneuver points ").Append(Signed(diff.ManeuverPointsDelta));

		foreach (var ability in diff.AddedAbilities)
			text.Append('\n')
				.Append("+ ")
				.Append(AbilityLabel(ability.Kind))
				.Append(" (")
				.Append(MerchantOfferDisplay.FaceLabel(ability.MountedOn))
				.Append(')');

		foreach (var ability in diff.RemovedAbilities)
			text.Append('\n')
				.Append("- ")
				.Append(AbilityLabel(ability.Kind))
				.Append(" (")
				.Append(MerchantOfferDisplay.FaceLabel(ability.MountedOn))
				.Append(')');

		foreach (var mount in diff.ChangedMounts)
		{
			text.Append('\n')
				.Append(AbilityLabel(mount.Mount.Kind))
				.Append(" (")
				.Append(MerchantOfferDisplay.FaceLabel(mount.Mount.Facet))
				.Append("):");
			if (mount.DamageTiersDelta != 0)
				text.Append(" dmg ").Append(Signed(mount.DamageTiersDelta));
			if (mount.RangeTiersDelta != 0)
				text.Append(" range ").Append(Signed(mount.RangeTiersDelta));
		}

		return text.ToString();
	}

	private static string Signed(int value) =>
		value > 0 ? $"+{value}" : value.ToString();

	private static string AbilityLabel(EAbilityKind kind) =>
		kind switch
		{
			EAbilityKind.ScrapDroneSwarm => "Scrap Drone Swarm",
			EAbilityKind.LightningCannon => "Lightning Cannon",
			EAbilityKind.MinerBay => "Miner Bay",
			EAbilityKind.VoidBombLauncher => "Void Bomb Launcher",
			EAbilityKind.GoopGun => "Goop Gun",
			_ => kind.ToString(),
		};
}
