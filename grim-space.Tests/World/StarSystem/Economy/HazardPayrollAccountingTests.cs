using GrimSpace.World.StarSystem.Economy;

namespace GrimSpace.Tests.World.StarSystem.Economy;

public sealed class HazardPayrollAccountingTests
{
	[Fact]
	public void Resolve_PaysAffordablePilots()
	{
		var roster = new[]
		{
			new HazardPayrollPilot("a", 0),
			new HazardPayrollPilot("b", 0),
		};

		var result = HazardPayrollAccounting.Resolve(
			roster,
			availableCredits: StrategicEconomyConfig.HazardPayCreditsPerPilot + 1,
			new Random(1));

		Assert.Single(result.PaidPilotIds);
		Assert.Single(result.Misses);
		Assert.Equal(StrategicEconomyConfig.HazardPayCreditsPerPilot, result.CreditsSpent);
	}

	[Fact]
	public void Resolve_PayClearsPathWithoutRefuse()
	{
		var roster = new[]
		{
			new HazardPayrollPilot("a", 3),
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
			new HazardPayrollPilot("a", 5),
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
}
