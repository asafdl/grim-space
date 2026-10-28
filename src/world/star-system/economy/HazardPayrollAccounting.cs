using GrimSpace.Run;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Economy;

public sealed record HazardPayrollPilot(
	string PilotId,
	int MissedHazardPeriods);

public sealed record HazardPayrollResult(
	IReadOnlyList<string> PaidPilotIds,
	IReadOnlyList<HazardPayrollPilot> Misses,
	IReadOnlyList<string> RefusedPilotIds,
	int CreditsSpent);

public static class HazardPayrollAccounting
{
	public static IReadOnlyList<HazardPayrollPilot> CandidatesFrom(RunPilotRegistry? pilots)
	{
		if (pilots is null)
			return [];

		var roster = new List<HazardPayrollPilot>();
		foreach (var pilot in pilots.Active)
		{
			if (string.IsNullOrEmpty(pilot.AssignedShipId))
				continue;

			roster.Add(new HazardPayrollPilot(pilot.Id, pilot.MissedHazardPeriods));
		}

		return roster;
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
		var due = StrategicEconomyConfig.HazardPayCreditsPerPilot;

		foreach (var candidate in order)
		{
			if (due > 0 && remaining >= due)
			{
				remaining -= due;
				paid.Add(candidate.PilotId);
				continue;
			}

			var missed = candidate.MissedHazardPeriods + 1;
			misses.Add(new HazardPayrollPilot(candidate.PilotId, missed));
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
