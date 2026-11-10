using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Economy;

public sealed record HazardPayrollPilot(
	string PilotId,
	int MissedHazardPeriods,
	int HazardPayCredits);

public sealed record HazardPayrollResult(
	IReadOnlyList<string> PaidPilotIds,
	IReadOnlyList<HazardPayrollPilot> Misses,
	IReadOnlyList<string> RefusedPilotIds,
	int CreditsSpent);

public static class HazardPayrollAccounting
{
	public static IReadOnlyList<HazardPayrollPilot> CandidatesFrom(
		RunPilotRegistry? pilots,
		IReadOnlyList<string>? deployedPilotIds = null)
	{
		if (pilots is null)
			return [];

		var roster = new List<HazardPayrollPilot>();
		foreach (var pilot in pilots.Assigned)
		{
			if (deployedPilotIds?.Contains(pilot.Id, StringComparer.Ordinal) == false)
				continue;

			roster.Add(new HazardPayrollPilot(
				pilot.Id,
				pilot.MissedHazardPeriods,
				HazardPayCreditsFor(pilot)));
		}

		return roster;
	}

	public static int HazardPayCreditsFor(Pilot pilot)
	{
		ArgumentNullException.ThrowIfNull(pilot);
		var due = StrategicEconomyConfig.HazardPayCreditsPerPilot;
		if (!PilotBackgroundCatalog.TryGet(pilot.BackgroundId, out var background))
			return due;

		foreach (var capabilityId in background.CapabilityIds)
		{
			if (capabilityId != EPilotCapabilityId.HazardPayDiscount)
				continue;

			due = ApplyHazardPayDiscount(due);
		}

		return due;
	}

	public static int ApplyHazardPayDiscount(int hazardPayCredits)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(hazardPayCredits);
		if (hazardPayCredits <= 0)
			return hazardPayCredits;

		var takenPercent = PilotSkillPercents.GearPsycho.HazardPayTakenPercent;
		ArgumentOutOfRangeException.ThrowIfGreaterThan(takenPercent, 100);
		return (int)System.Math.Round(
			hazardPayCredits * takenPercent / 100m,
			System.MidpointRounding.AwayFromZero);
	}

	public static HazardPayrollResult Resolve(
		IReadOnlyList<HazardPayrollPilot> wouldDeploy,
		int availableCredits,
		Random rng)
	{
		ArgumentNullException.ThrowIfNull(wouldDeploy);
		ArgumentOutOfRangeException.ThrowIfNegative(availableCredits);
		ArgumentNullException.ThrowIfNull(rng);

		var order = wouldDeploy.ToList();
		Shuffle(order, rng);

		var paid = new List<string>();
		var misses = new List<HazardPayrollPilot>();
		var refused = new List<string>();
		var remaining = availableCredits;

		foreach (var candidate in order)
		{
			var due = System.Math.Max(0, candidate.HazardPayCredits);
			if (due > 0 && remaining >= due)
			{
				remaining -= due;
				paid.Add(candidate.PilotId);
				continue;
			}

			var missed = candidate.MissedHazardPeriods + 1;
			misses.Add(new HazardPayrollPilot(candidate.PilotId, missed, candidate.HazardPayCredits));
			var refuseChance = StrategicEconomyConfig.RefuseChancePercent(missed);
			if (refuseChance > 0 && rng.Next(100) < refuseChance)
				refused.Add(candidate.PilotId);
		}

		return new HazardPayrollResult(
			paid,
			misses,
			refused,
			availableCredits - remaining);
	}

	public static int EstimateCredits(IReadOnlyList<HazardPayrollPilot> wouldDeploy)
	{
		ArgumentNullException.ThrowIfNull(wouldDeploy);
		var total = 0;
		foreach (var candidate in wouldDeploy)
			total += System.Math.Max(0, candidate.HazardPayCredits);
		return total;
	}

	public static int EstimateCredits(int wouldDeployCount) =>
		System.Math.Max(0, wouldDeployCount) * StrategicEconomyConfig.HazardPayCreditsPerPilot;

	public static ResourceBundle CreditSpend(int credits) =>
		credits <= 0
			? ResourceBundle.Empty
			: ResourceBundle.Of(ResourceId.Credits, -credits);

	private static void Shuffle<T>(IList<T> values, Random rng)
	{
		for (var i = values.Count - 1; i > 0; i--)
		{
			var j = rng.Next(i + 1);
			(values[i], values[j]) = (values[j], values[i]);
		}
	}
}
