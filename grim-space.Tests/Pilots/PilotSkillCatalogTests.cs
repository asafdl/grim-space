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
	}
}
