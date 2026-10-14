using GrimSpace.Pilots;

namespace GrimSpace.Tests.Pilots;

[IntegrationTestSuite]
public sealed class PilotSkillCatalogTests
{
	[Fact]
	public void Get_ReturnsShieldsUpMetadata()
	{
		var definition = PilotSkillCatalog.Get(EPilotSkillId.ShieldsUp);

		Assert.Equal(EPilotSkillId.ShieldsUp, definition.Id);
		Assert.Equal(EPilotSkillTag.BattleAbility, definition.Tag);
		Assert.Equal("Shields Up", definition.Name);
		Assert.Equal(
			"Boost shield faces until your next turn; pay AP when that turn begins.",
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
		Assert.Contains(PilotSkillCatalog.All, skill => skill.Id == EPilotSkillId.IonizedPayload);
		Assert.Contains(PilotSkillCatalog.All, skill => skill.Id == EPilotSkillId.DefensiveManeuvers);
		Assert.Contains(PilotSkillCatalog.All, skill => skill.Id == EPilotSkillId.DronesYearnForMines);
		Assert.Contains(PilotSkillCatalog.All, skill => skill.Id == EPilotSkillId.IKnowABot);
	}

	[Fact]
	public void Get_ReturnsDefensiveManeuversMetadata()
	{
		var definition = PilotSkillCatalog.Get(EPilotSkillId.DefensiveManeuvers);

		Assert.Equal(EPilotSkillTag.BattlePassive, definition.Tag);
		Assert.Equal("Defensive Maneuvers", definition.Name);
		Assert.Equal(
			"If you spend no maneuver points during your turn, gain a 25% chance to deflect 1 damage from each hit until your next turn ends.",
			definition.Description);
		Assert.Equal(2, definition.Tier);
		Assert.Empty(definition.Prerequisites);
		Assert.Equal(3, definition.MinimumEarnedLevel);
	}

	[Fact]
	public void Get_ReturnsIonizedPayloadMetadata()
	{
		var definition = PilotSkillCatalog.Get(EPilotSkillId.IonizedPayload);

		Assert.Equal(EPilotSkillTag.BattlePassive, definition.Tag);
		Assert.Equal("Ionized Payload", definition.Name);
		Assert.Equal("Lightning weapons deal 1 additional damage to shields.", definition.Description);
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

		Assert.Equal(EPilotSkillTag.StrategicPassive, definition.Tag);
		Assert.Equal(name, definition.Name);
		Assert.Equal(description, definition.Description);
		Assert.Equal(tier, definition.Tier);
		Assert.Empty(definition.Prerequisites);
		Assert.Equal(minimumEarnedLevel, definition.MinimumEarnedLevel);
	}
}
