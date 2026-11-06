using GrimSpace.Pilots;
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
		Random rng,
		int fleetSalaryDiscountStacks = 0)
	{
		ArgumentNullException.ThrowIfNull(roster);
		ArgumentOutOfRangeException.ThrowIfNegative(availableCredits);
		ArgumentNullException.ThrowIfNull(rng);
		ArgumentOutOfRangeException.ThrowIfNegative(fleetSalaryDiscountStacks);

		var order = roster.ToList();
		Shuffle(order, rng);

		var paid = new List<string>();
		var misses = new List<SalaryPayrollMiss>();
		var remaining = availableCredits;

		foreach (var candidate in order)
		{
			var due = ApplyFleetSalaryDiscount(
				StrategicEconomyConfig.SalaryFor(
					candidate.BaseSalaryCredits,
					candidate.ClaimedLevel),
				fleetSalaryDiscountStacks);
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

	public static int ApplyFleetSalaryDiscount(int salaryCredits, int discountStacks)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(salaryCredits);
		ArgumentOutOfRangeException.ThrowIfNegative(discountStacks);
		if (salaryCredits <= 0 || discountStacks <= 0)
			return salaryCredits;

		var discountPercent = PilotSkillPercents.BeanCounter.FleetSalaryDiscountPercent;
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(discountPercent, 100);
		var retained = (100 - discountPercent) / 100m;
		var amount = (decimal)salaryCredits;
		for (var i = 0; i < discountStacks; i++)
			amount *= retained;

		return (int)System.Math.Round(amount, System.MidpointRounding.AwayFromZero);
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
