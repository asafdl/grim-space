using GrimSpace.World.StarSystem.Economy;

namespace GrimSpace.Tests.World.StarSystem.Economy;

public sealed class SalaryPayrollAccountingTests
{
	[Fact]
	public void Resolve_PaysAffordablePilotsInShuffledOrder()
	{
		var roster = new[]
		{
			new SalaryPayrollCandidate("a", BaseSalaryCredits: 10, ClaimedLevel: 1, MissedSalaryPeriods: 0),
			new SalaryPayrollCandidate("b", BaseSalaryCredits: 10, ClaimedLevel: 1, MissedSalaryPeriods: 0),
		};

		var payroll = SalaryPayrollAccounting.Resolve(roster, availableCredits: 15, new Random(1));

		Assert.Single(payroll.PaidPilotIds);
		Assert.Single(payroll.Misses);
		Assert.Equal(10, payroll.CreditsSpent);
		Assert.Equal(1, payroll.Misses[0].MissedSalaryPeriods);
		Assert.False(payroll.Misses[0].Departed);
	}

	[Fact]
	public void Resolve_ClearsPayPathWhenFullyFunded()
	{
		var roster = new[]
		{
			new SalaryPayrollCandidate("a", 10, 1, 2),
			new SalaryPayrollCandidate("b", 10, 1, 0),
		};

		var payroll = SalaryPayrollAccounting.Resolve(roster, availableCredits: 100, new Random(2));

		Assert.Equal(2, payroll.PaidPilotIds.Count);
		Assert.Empty(payroll.Misses);
		Assert.Equal(20, payroll.CreditsSpent);
	}

	[Fact]
	public void Resolve_FirstMissDoesNotDepart()
	{
		var roster = new[]
		{
			new SalaryPayrollCandidate("a", 10, 1, 0),
		};

		var payroll = SalaryPayrollAccounting.Resolve(roster, availableCredits: 0, new Random(3));

		var miss = Assert.Single(payroll.Misses);
		Assert.Equal(1, miss.MissedSalaryPeriods);
		Assert.False(miss.Departed);
	}

	[Fact]
	public void Resolve_CanDepartOnLaterMisses()
	{
		var roster = new[]
		{
			new SalaryPayrollCandidate("a", 10, 1, 5),
		};

		var departed = false;
		for (var seed = 0; seed < 200 && !departed; seed++)
		{
			var payroll = SalaryPayrollAccounting.Resolve(roster, availableCredits: 0, new Random(seed));
			departed = Assert.Single(payroll.Misses).Departed;
		}

		Assert.True(departed);
	}
}
