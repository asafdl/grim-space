using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Economy;

public sealed record SalaryPayrollCandidate(
	string PilotId,
	int BaseSalaryCredits,
	int ClaimedLevel,
	int MissedSalaryPeriods);

public sealed record SalaryPayrollMiss(
	string PilotId,
	int MissedSalaryPeriods,
	bool Departed);

public sealed record PilotSalaryPayroll(
	IReadOnlyList<string> PaidPilotIds,
	IReadOnlyList<SalaryPayrollMiss> Misses,
	int CreditsSpent);

public static class SalaryPayrollAccounting
{
	public static PilotSalaryPayroll Resolve(
		IReadOnlyList<SalaryPayrollCandidate> roster,
		int availableCredits,
		Random rng)
	{
		ArgumentNullException.ThrowIfNull(roster);
		ArgumentOutOfRangeException.ThrowIfNegative(availableCredits);
		ArgumentNullException.ThrowIfNull(rng);

		var order = roster.ToList();
		Shuffle(order, rng);

		var paid = new List<string>();
		var misses = new List<SalaryPayrollMiss>();
		var remaining = availableCredits;

		foreach (var candidate in order)
		{
			var due = StrategicEconomyConfig.SalaryFor(
				candidate.BaseSalaryCredits,
				candidate.ClaimedLevel);
			if (due > 0 && remaining >= due)
			{
				remaining -= due;
				paid.Add(candidate.PilotId);
				continue;
			}

			var missed = candidate.MissedSalaryPeriods + 1;
			var departChance = StrategicEconomyConfig.DepartChancePercent(missed);
			var departed = departChance > 0 && rng.Next(100) < departChance;
			misses.Add(new SalaryPayrollMiss(candidate.PilotId, missed, departed));
		}

		return new PilotSalaryPayroll(paid, misses, availableCredits - remaining);
	}

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
