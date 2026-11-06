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

		Assert.Equal(10, backgrounds.Length);
		Assert.Contains(backgrounds, background => background.Id == EPilotBackgroundId.ScrapDrone);
		Assert.Contains(backgrounds, background => background.Id == EPilotBackgroundId.OptimalityEnforcer);
		Assert.Contains(backgrounds, background => background.Id == EPilotBackgroundId.CorporateSlave);
		Assert.Contains(backgrounds, background => background.Id == EPilotBackgroundId.PodRacer);
		Assert.Contains(backgrounds, background => background.Id == EPilotBackgroundId.AsteroidSurfer);
		Assert.Contains(backgrounds, background => background.Id == EPilotBackgroundId.RetiredMachineManager);
		Assert.Contains(backgrounds, background => background.Id == EPilotBackgroundId.CyberJunkie);
		Assert.Contains(backgrounds, background => background.Id == EPilotBackgroundId.HydraulicAbuser);
		Assert.Contains(backgrounds, background => background.Id == EPilotBackgroundId.BeanCounter);
		Assert.Contains(backgrounds, background => background.Id == EPilotBackgroundId.GearPsycho);
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
	public void PodRacer_GrantsHeatGainReduction()
	{
		var definition = PilotBackgroundCatalog.Get(EPilotBackgroundId.PodRacer);

		Assert.Equal("Pod Racer", definition.Name);
		Assert.Equal([EPilotCapabilityId.HeatGainReduction], definition.CapabilityIds);
		Assert.Contains(
			$"-{PilotSkillPercents.PodRacer.HeatGainReductionPercent}%",
			definition.Tooltip.Details,
			StringComparison.Ordinal);
	}

	[Fact]
	public void AsteroidSurfer_GrantsDamageTradeoffCapabilities()
	{
		var definition = PilotBackgroundCatalog.Get(EPilotBackgroundId.AsteroidSurfer);

		Assert.Equal("Asteroid Surfer", definition.Name);
		Assert.Equal(
			[
				EPilotCapabilityId.OutgoingDamageBonus,
				EPilotCapabilityId.IncreasedHullDamageTaken,
			],
			definition.CapabilityIds);
		Assert.Contains(
			$"+{PilotSkillPercents.AsteroidSurfer.OutgoingDamageBonusPercent}%",
			definition.Tooltip.Details,
			StringComparison.Ordinal);
		Assert.Contains(
			$"+{PilotSkillPercents.AsteroidSurfer.HullDamageTakenPercent - 100}%",
			definition.Tooltip.Details,
			StringComparison.Ordinal);
	}

	[Fact]
	public void RetiredMachineManager_GrantsSpawnedAllyDamageBonus()
	{
		var definition = PilotBackgroundCatalog.Get(EPilotBackgroundId.RetiredMachineManager);

		Assert.Equal("Retired Machine Manager", definition.Name);
		Assert.Equal([EPilotCapabilityId.SpawnedAllyDamageBonus], definition.CapabilityIds);
		Assert.Contains(
			$"+{PilotSkillPercents.RetiredMachineManager.SpawnedAllyDamageBonusPercent}%",
			definition.Tooltip.Details,
			StringComparison.Ordinal);
	}

	[Fact]
	public void CyberJunkie_GrantsXpAndHeatClearBonuses()
	{
		var definition = PilotBackgroundCatalog.Get(EPilotBackgroundId.CyberJunkie);

		Assert.Equal("Cyber Junkie", definition.Name);
		Assert.Equal(
			[
				EPilotCapabilityId.ExperienceGainBonus,
				EPilotCapabilityId.HeatClearBonus,
			],
			definition.CapabilityIds);
		Assert.Contains(
			$"+{PilotSkillPercents.CyberJunkie.ExperienceGainBonusPercent}% XP",
			definition.Tooltip.Details,
			StringComparison.Ordinal);
		Assert.Contains(
			$"+{PilotSkillPercents.CyberJunkie.HeatClearBonusPercent}%",
			definition.Tooltip.Details,
			StringComparison.Ordinal);
	}

	[Fact]
	public void HydraulicAbuser_GrantsDamageResistance()
	{
		var definition = PilotBackgroundCatalog.Get(EPilotBackgroundId.HydraulicAbuser);

		Assert.Equal("Hydraulic Abuser", definition.Name);
		Assert.Equal([EPilotCapabilityId.DamageResistance], definition.CapabilityIds);
		Assert.Contains(
			$"+{100 - PilotSkillPercents.HydraulicAbuser.DamageTakenPercent}%",
			definition.Tooltip.Details,
			StringComparison.Ordinal);
	}

	[Fact]
	public void BeanCounter_GrantsFleetSalaryDiscount()
	{
		var definition = PilotBackgroundCatalog.Get(EPilotBackgroundId.BeanCounter);

		Assert.Equal("Bean Counter", definition.Name);
		Assert.Equal([EPilotCapabilityId.FleetSalaryDiscount], definition.CapabilityIds);
		Assert.Contains(
			$"-{PilotSkillPercents.BeanCounter.FleetSalaryDiscountPercent}%",
			definition.Tooltip.Details,
			StringComparison.Ordinal);
	}

	[Fact]
	public void GearPsycho_GrantsHazardPayDiscount()
	{
		var definition = PilotBackgroundCatalog.Get(EPilotBackgroundId.GearPsycho);

		Assert.Equal("Gear Psycho", definition.Name);
		Assert.Equal([EPilotCapabilityId.HazardPayDiscount], definition.CapabilityIds);
		Assert.Contains(
			$"-{100 - PilotSkillPercents.GearPsycho.HazardPayTakenPercent}%",
			definition.Tooltip.Details,
			StringComparison.Ordinal);
	}

	[Fact]
	public void Roll_ReturnsRegisteredBackground()
	{
		var definition = PilotBackgroundCatalog.Roll(new Random(42));

		Assert.Contains(PilotBackgroundCatalog.All, background => background.Id == definition.Id);
	}
}
