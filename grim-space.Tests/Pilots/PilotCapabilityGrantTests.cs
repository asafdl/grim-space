using GrimSpace.Battle.PilotCapabilities;
using GrimSpace.Pilots;
using GrimSpace.World.StarSystem.PilotCapabilities;

namespace GrimSpace.Tests.Pilots;

[IntegrationTestSuite]
public sealed class PilotCapabilityGrantTests
{
	[Fact]
	public void FromQuirks_PlaceholderQuirksGrantNoCapabilities()
	{
		var grants = PilotCapabilityGrant.FromQuirks(
			"pilot-a",
			[EPilotQuirkId.PlaceholderPositive, EPilotQuirkId.PlaceholderNegative]);

		Assert.Empty(grants);
	}

	[Fact]
	public void FromQuirks_EmitsQuirkSourceGrants()
	{
		var grants = PilotCapabilityGrant.FromQuirks(
			"pilot-a",
			[EPilotQuirkId.GrantProbePositive],
			BattlePilotCapabilityPipelineFactory.Supports);

		var grant = Assert.Single(grants);
		Assert.Equal(EPilotCapabilityId.LightningShieldDamageBonus, grant.CapabilityId);
		Assert.Equal("pilot-a", grant.PilotId);
		Assert.Equal(EPilotCapabilitySourceKind.Quirk, grant.Source.Kind);
		Assert.Equal(EPilotQuirkId.GrantProbePositive.ToString(), grant.Source.Id);
	}

	[Fact]
	public void FromQuirks_FiltersByCapabilityPredicate()
	{
		var grants = PilotCapabilityGrant.FromQuirks(
			"pilot-a",
			[EPilotQuirkId.GrantProbeNegative],
			BattlePilotCapabilityPipelineFactory.Supports);

		Assert.Empty(grants);
	}

	[Fact]
	public void FromQuirks_StrategicProbePassesStrategicPredicate()
	{
		var grants = PilotCapabilityGrant.FromQuirks(
			"pilot-a",
			[EPilotQuirkId.GrantProbeNegative],
			StarMapPilotCapabilityPipelineFactory.Supports);

		Assert.Equal(EPilotCapabilityId.ScrapRewardBonus, Assert.Single(grants).CapabilityId);
	}
}
