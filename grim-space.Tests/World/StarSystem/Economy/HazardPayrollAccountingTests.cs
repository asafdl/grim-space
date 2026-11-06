using GrimSpace.Pilots;
using GrimSpace.World.StarSystem.Economy;

namespace GrimSpace.Tests.World.StarSystem.Economy;

public sealed class HazardPayrollAccountingTests
{
	private static readonly int FullPay = StrategicEconomyConfig.HazardPayCreditsPerPilot;

	[Fact]
	public void Resolve_PaysAffordablePilots()
	{
		var roster = new[]
		{
			new HazardPayrollPilot("a", 0, FullPay),
			new HazardPayrollPilot("b", 0, FullPay),
		};

		var result = HazardPayrollAccounting.Resolve(
			roster,
			availableCredits: FullPay + 1,
			new Random(1));

		Assert.Single(result.PaidPilotIds);
		Assert.Single(result.Misses);
		Assert.Equal(FullPay, result.CreditsSpent);
	}

	[Fact]
	public void Resolve_PayClearsPathWithoutRefuse()
	{
		var roster = new[]
		{
			new HazardPayrollPilot("a", 3, FullPay),
		};

		var result = HazardPayrollAccounting.Resolve(
			roster,
			availableCredits: 100,
			new Random(2));

		Assert.Equal(["a"], result.PaidPilotIds);
		Assert.Empty(result.Misses);
		Assert.Empty(result.RefusedPilotIds);
	}

	[Fact]
	public void Resolve_UnpaidCanRefuseForThisEngagement()
	{
		var roster = new[]
		{
			new HazardPayrollPilot("a", 5, FullPay),
		};

		var refused = false;
		for (var seed = 0; seed < 200 && !refused; seed++)
		{
			var result = HazardPayrollAccounting.Resolve(roster, availableCredits: 0, new Random(seed));
			refused = result.RefusedPilotIds.Contains("a");
			Assert.Equal(6, Assert.Single(result.Misses).MissedHazardPeriods);
		}

		Assert.True(refused);
	}

	[Fact]
	public void EstimateCredits_ScalesWithWouldDeployCount() =>
		Assert.Equal(
			30,
			HazardPayrollAccounting.EstimateCredits(3));

	[Fact]
	public void EstimateCredits_SumsPerPilotHazardPay()
	{
		var roster = new[]
		{
			new HazardPayrollPilot("a", 0, FullPay),
			new HazardPayrollPilot("b", 0, FullPay / 2),
		};

		Assert.Equal(FullPay + FullPay / 2, HazardPayrollAccounting.EstimateCredits(roster));
	}

	[Fact]
	public void ApplyHazardPayDiscount_HalvesPay() =>
		Assert.Equal(
			FullPay / 2,
			HazardPayrollAccounting.ApplyHazardPayDiscount(FullPay));

	[Fact]
	public void Resolve_UsesPerPilotHazardPayCredits()
	{
		var discounted = HazardPayrollAccounting.ApplyHazardPayDiscount(FullPay);
		var roster = new[]
		{
			new HazardPayrollPilot("psycho", 0, discounted),
			new HazardPayrollPilot("normal", 0, FullPay),
		};

		var result = HazardPayrollAccounting.Resolve(
			roster,
			availableCredits: discounted + FullPay,
			new Random(4));

		Assert.Equal(2, result.PaidPilotIds.Count);
		Assert.Equal(discounted + FullPay, result.CreditsSpent);
	}
}
