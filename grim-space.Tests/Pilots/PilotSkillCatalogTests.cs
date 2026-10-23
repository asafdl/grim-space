using GrimSpace.Pilots;
using GrimSpace.Battle.PilotCapabilities;
using GrimSpace.World.StarSystem.PilotCapabilities;

namespace GrimSpace.Tests.Pilots;

[IntegrationTestSuite]
public sealed class PilotSkillCatalogTests
{
	[Fact]
	public void Get_ReturnsShieldsUpMetadata()
	{
		var definition = PilotSkillCatalog.Get(EPilotSkillId.ShieldsUp);

		Assert.Equal(EPilotSkillId.ShieldsUp, definition.Id);
		Assert.Equal([EPilotCapabilityId.ShieldBoost], definition.CapabilityIds);
		Assert.Equal("Shields Up", definition.Name);
		Assert.Equal(
			$"Boost each shield face by {PilotSkillPercents.ShieldsUp.TemporaryShieldPercentOfFaceMax}% of its maximum until your next turn; pay AP when that turn begins.",
			definition.Description);
		Assert.Equal(1, definition.Tier);
		Assert.Empty(definition.Prerequisites);
		Assert.Equal(1, definition.MinimumEarnedLevel);
	}

	[Fact]
	public void Get_ThrowsForUnknownSkill()
	{
		Assert.Throws<KeyNotFoundException>(() => PilotSkillCatalog.Get((EPilotSkillId)999));
	}

	[Fact]
	public void All_EnumeratesRegisteredSkills()
	{
		Assert.Contains(PilotSkillCatalog.All, skill => skill.Id == EPilotSkillId.ShieldsUp);
		Assert.Contains(PilotSkillCatalog.All, skill => skill.Id == EPilotSkillId.DodgeThis);
		Assert.Contains(PilotSkillCatalog.All, skill => skill.Id == EPilotSkillId.IonizedPayload);
		Assert.Contains(PilotSkillCatalog.All, skill => skill.Id == EPilotSkillId.DefensiveManeuvers);
		Assert.Contains(PilotSkillCatalog.All, skill => skill.Id == EPilotSkillId.InTheFray);
		Assert.Contains(PilotSkillCatalog.All, skill => skill.Id == EPilotSkillId.DronesYearnForMines);
		Assert.Contains(PilotSkillCatalog.All, skill => skill.Id == EPilotSkillId.IKnowABot);
	}

	[Fact]
	public void EverySkill_GrantsSupportedCapabilities()
	{
		foreach (var skill in PilotSkillCatalog.All)
		{
			Assert.NotEmpty(skill.CapabilityIds);
			Assert.All(
				skill.CapabilityIds,
				capabilityId => Assert.True(
					BattlePilotCapabilityPipelineFactory.Supports(capabilityId)
					|| StarMapPilotCapabilityPipelineFactory.Supports(capabilityId)));
		}
	}

	[Fact]
	public void Get_ReturnsInTheFrayMetadata()
	{
		var definition = PilotSkillCatalog.Get(EPilotSkillId.InTheFray);

		Assert.Equal([EPilotCapabilityId.AlliedDamageReduction], definition.CapabilityIds);
		Assert.Equal("In The Fray", definition.Name);
		Assert.Equal(
			"Take 50% less damage from allied units. Self-damage is unaffected.",
			definition.Description);
		Assert.Equal(3, definition.Tier);
		Assert.Empty(definition.Prerequisites);
		Assert.Equal(4, definition.MinimumEarnedLevel);
	}

	[Fact]
	public void Get_ReturnsDodgeThisMetadata()
	{
		var definition = PilotSkillCatalog.Get(EPilotSkillId.DodgeThis);

		Assert.Equal([EPilotCapabilityId.AdjacentDamageBonus], definition.CapabilityIds);
		Assert.Equal("Dodge This", definition.Name);
		Assert.Equal(
			"Deal 50% additional damage when hitting an adjacent target (150% total weapon damage).",
			definition.Description);
		Assert.Equal(1, definition.Tier);
		Assert.Empty(definition.Prerequisites);
		Assert.Equal(2, definition.MinimumEarnedLevel);
	}

	[Fact]
	public void Get_ReturnsDefensiveManeuversMetadata()
	{
		var definition = PilotSkillCatalog.Get(EPilotSkillId.DefensiveManeuvers);

		Assert.Equal(
			[EPilotCapabilityId.StationaryDamageDeflection],
			definition.CapabilityIds);
		Assert.Equal("Defensive Maneuvers", definition.Name);
		Assert.Equal(
			"If you spend no maneuver points during your turn, gain a 25% chance to deflect 50% damage from each hit until your next turn ends.",
			definition.Description);
		Assert.Equal(2, definition.Tier);
		Assert.Empty(definition.Prerequisites);
		Assert.Equal(3, definition.MinimumEarnedLevel);
	}

	[Fact]
	public void Get_ReturnsIonizedPayloadMetadata()
	{
		var definition = PilotSkillCatalog.Get(EPilotSkillId.IonizedPayload);

		Assert.Equal(
			[EPilotCapabilityId.LightningShieldDamageBonus],
			definition.CapabilityIds);
		Assert.Equal("Ionized Payload", definition.Name);
		Assert.Equal(
			"Lightning weapons deal 100% additional damage to shields.",
			definition.Description);
		Assert.Equal(2, definition.Tier);
		Assert.Empty(definition.Prerequisites);
		Assert.Equal(3, definition.MinimumEarnedLevel);
	}

	[Theory]
	[InlineData(
		EPilotSkillId.DronesYearnForMines,
		"The Drones Yearn for the Mines",
		"Gain 15% more Scrap Alloy from destroyed ships and successful wreckage investigations.",
		1,
		2)]
	[InlineData(
		EPilotSkillId.IKnowABot,
		"I Know a Bot",
		"Ship upgrades cost 10% fewer Credits and Scrap Alloy. Industrial Core costs are unchanged.",
		3,
		4)]
	public void Get_ReturnsStrategicPassiveMetadata(
		EPilotSkillId id,
		string name,
		string description,
		int tier,
		int minimumEarnedLevel)
	{
		var definition = PilotSkillCatalog.Get(id);

		var capability = Assert.Single(definition.CapabilityIds);
		Assert.True(StarMapPilotCapabilityPipelineFactory.Supports(capability));
		Assert.Equal(name, definition.Name);
		Assert.Equal(description, definition.Description);
		Assert.Equal(tier, definition.Tier);
		Assert.Empty(definition.Prerequisites);
		Assert.Equal(minimumEarnedLevel, definition.MinimumEarnedLevel);
	}
}
