using GrimSpace.Pilots;

namespace GrimSpace.Tests.Pilots;

[IntegrationTestSuite]
public sealed class PilotBackgroundCatalogTests
{
	[Fact]
	public void Get_ThrowsForUnknownBackground()
	{
		Assert.Throws<KeyNotFoundException>(() =>
			PilotBackgroundCatalog.Get((EPilotBackgroundId)999));
	}

	[Fact]
	public void All_EnumeratesRegisteredBackgrounds()
	{
		var backgrounds = PilotBackgroundCatalog.All.ToArray();

		Assert.Equal(6, backgrounds.Length);
		Assert.Contains(backgrounds, background => background.Id == EPilotBackgroundId.ScrapDrone);
		Assert.Contains(backgrounds, background => background.Id == EPilotBackgroundId.OptimalityEnforcer);
		Assert.Contains(backgrounds, background => background.Id == EPilotBackgroundId.CorporateSlave);
		Assert.Contains(backgrounds, background => background.Id == EPilotBackgroundId.PodRacer);
		Assert.Contains(backgrounds, background => background.Id == EPilotBackgroundId.AsteroidSurfer);
		Assert.Contains(backgrounds, background => background.Id == EPilotBackgroundId.SynthSinger);
	}

	[Fact]
	public void ScrapDrone_GrantsBattleAndStrategicCapabilities()
	{
		var definition = PilotBackgroundCatalog.Get(EPilotBackgroundId.ScrapDrone);

		Assert.Equal("Scrap Drone", definition.Name);
		Assert.Equal(
			[
				EPilotCapabilityId.ScrapDroneSwarmDamageBonus,
				EPilotCapabilityId.MinorScrapRewardBonus,
			],
			definition.CapabilityIds);
	}

	[Fact]
	public void OptimalityEnforcer_GrantsFactionContractCapability()
	{
		var definition = PilotBackgroundCatalog.Get(EPilotBackgroundId.OptimalityEnforcer);

		Assert.Equal("Optimality Enforcer", definition.Name);
		Assert.Equal(
			[EPilotCapabilityId.OptimalityContractCreditBonus],
			definition.CapabilityIds);
	}

	[Fact]
	public void CorporateSlave_GrantsInitiativePenalty()
	{
		var definition = PilotBackgroundCatalog.Get(EPilotBackgroundId.CorporateSlave);

		Assert.Equal("Corporate Slave", definition.Name);
		Assert.Equal([EPilotCapabilityId.InitiativePenalty], definition.CapabilityIds);
	}

	[Fact]
	public void PodRacer_HasDeferredSmallShipManeuverabilityEffect()
	{
		var definition = PilotBackgroundCatalog.Get(EPilotBackgroundId.PodRacer);

		Assert.Equal("Pod Racer", definition.Name);
		Assert.Empty(definition.CapabilityIds);
		Assert.Contains("(TBD)", definition.Description, StringComparison.Ordinal);
	}

	[Fact]
	public void AsteroidSurfer_HasDeferredDamageTradeoff()
	{
		var definition = PilotBackgroundCatalog.Get(EPilotBackgroundId.AsteroidSurfer);

		Assert.Equal("Asteroid Surfer", definition.Name);
		Assert.Empty(definition.CapabilityIds);
		Assert.Contains("15% more damage", definition.Description, StringComparison.Ordinal);
		Assert.Contains("10% more damage", definition.Description, StringComparison.Ordinal);
		Assert.Contains("(TBD)", definition.Description, StringComparison.Ordinal);
	}

	[Fact]
	public void SynthSinger_HasDeferredAddictionQuirkWeighting()
	{
		var definition = PilotBackgroundCatalog.Get(EPilotBackgroundId.SynthSinger);

		Assert.Equal("Synth Singer", definition.Name);
		Assert.Empty(definition.CapabilityIds);
		Assert.Contains("addiction quirks", definition.Description, StringComparison.Ordinal);
		Assert.Contains("(TBD)", definition.Description, StringComparison.Ordinal);
	}

	[Fact]
	public void Roll_ReturnsRegisteredBackground()
	{
		var definition = PilotBackgroundCatalog.Roll(new Random(42));

		Assert.Contains(PilotBackgroundCatalog.All, background => background.Id == definition.Id);
	}
}
