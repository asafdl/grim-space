using GrimSpace.Pilots;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.World.StarSystem.Presentation.Facilities;

namespace GrimSpace.Tests.World.StarSystem.Merchants;

[StarSystemTestSuite]
public sealed class PilotHireDisplayTests
{
	[Fact]
	public void CardTitle_UsesBackgroundName()
	{
		Assert.Equal(
			"Scrap Drone",
			PilotHireDisplay.CardTitle(EPilotBackgroundId.ScrapDrone));
	}

	[Fact]
	public void CardSubtitle_IncludesPilotNameAndLevel()
	{
		var profile = new PilotRecruitProfile(
			"Rin",
			"pilot_01",
			EPilotBackgroundId.PodRacer,
			EPilotQuirkId.Charismatic,
			EPilotQuirkId.NeuroDrugAddict,
			experiencePoints: 0,
			claimedLevel: 1);

		Assert.Equal("Rin · Lv 1", PilotHireDisplay.CardSubtitle(profile));
	}

	[Fact]
	public void SalaryAmount_UsesLevelAdjustedSalary()
	{
		var profile = new PilotRecruitProfile(
			"Rin",
			"pilot_01",
			EPilotBackgroundId.PodRacer,
			claimedLevel: 3,
			baseSalaryCredits: 60);

		Assert.Equal(
			StrategicEconomyConfig.SalaryFor(60, 3),
			PilotHireDisplay.SalaryAmount(profile));
	}

	[Fact]
	public void ShipPackageLine_IncludesChassisLabelAndGearTier()
	{
		Assert.Equal(
			"Repurposed Miner (T2)",
			PilotHireDisplay.ShipPackageLine(EType.RepurposedMiner, EShipGearTier.T2));
	}

	[Fact]
	public void ShouldShowRecruitedQuirkIcon_IsFalseForNullOrWhilePoolsAreEmpty()
	{
		Assert.False(PilotHireDisplay.ShouldShowRecruitedQuirkIcon(
			null,
			EPilotQuirkPolarity.Positive));
		Assert.False(PilotHireDisplay.ShouldShowRecruitedQuirkIcon(
			EPilotQuirkId.Charismatic,
			EPilotQuirkPolarity.Positive));
		Assert.False(PilotHireDisplay.ShouldShowRecruitedQuirkIcon(
			EPilotQuirkId.NeuroDrugAddict,
			EPilotQuirkPolarity.Negative));
	}
}
