using GrimSpace.Battle.PilotCapabilities;
using GrimSpace.Pilots;
using GrimSpace.World.StarSystem.PilotCapabilities;

namespace GrimSpace.Tests.Pilots;

[IntegrationTestSuite]
public sealed class PilotCapabilityGrantTests
{
	[Fact]
	public void FromBackground_EmitsBackgroundSourceGrants()
	{
		var grants = PilotCapabilityGrant.FromBackground(
			"pilot-a",
			EPilotBackgroundId.ScrapDrone);

		Assert.Equal(2, grants.Count);
		Assert.All(grants, grant =>
		{
			Assert.Equal("pilot-a", grant.PilotId);
			Assert.Equal(EPilotCapabilitySourceKind.Background, grant.Source.Kind);
			Assert.Equal(EPilotBackgroundId.ScrapDrone.ToString(), grant.Source.Id);
		});
	}

	[Fact]
	public void FromBackground_FiltersBySubsystemSupport()
	{
		var battle = PilotCapabilityGrant.FromBackground(
			"pilot-a",
			EPilotBackgroundId.ScrapDrone,
			BattlePilotCapabilityPipelineFactory.Supports);
		var strategic = PilotCapabilityGrant.FromBackground(
			"pilot-a",
			EPilotBackgroundId.ScrapDrone,
			StarMapPilotCapabilityPipelineFactory.Supports);

		Assert.Equal(EPilotCapabilityId.ScrapDroneSwarmDamageBonus, Assert.Single(battle).CapabilityId);
		Assert.Equal(EPilotCapabilityId.MinorScrapRewardBonus, Assert.Single(strategic).CapabilityId);
	}

	[Fact]
	public void FromQuirks_WithDeferredQuirk_ReturnsNoCapabilityGrants()
	{
		var grants = PilotCapabilityGrant.FromQuirks(
			"pilot-a",
			[EPilotQuirkId.Charismatic],
			BattlePilotCapabilityPipelineFactory.Supports);

		Assert.Empty(grants);
	}

	[Fact]
	public void FromQuirks_FiltersByCapabilityPredicate()
	{
		var grants = PilotCapabilityGrant.FromQuirks(
			"pilot-a",
			[EPilotQuirkId.NeuroDrugAddict],
			BattlePilotCapabilityPipelineFactory.Supports);

		Assert.Empty(grants);
	}
}
