using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.PilotSkills;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.World.StarSystem.PilotSkills;

public sealed class StrategicPilotSkillModifiersTests
{
	[Fact]
	public void ApplySalvageBonus_StacksWithoutChangingOtherResources()
	{
		var map = StarMap.Create(1);
		var grants = StackGrants(EPilotSkillId.DronesYearnForMines, 2);
		var resources = ResourceBundle.Create(
			(ResourceId.ScrapAlloy, 101),
			(ResourceId.IndustrialCore, 1));

		var modified = StrategicPilotSkillModifiers.ApplySalvageBonus(map, resources, grants);

		AssertResource(modified, ResourceId.ScrapAlloy, 131);
		AssertResource(modified, ResourceId.IndustrialCore, 1);
	}

	[Fact]
	public void ModifiersWithoutLearnedSkills_PreserveBundles()
	{
		var map = StarMap.Create(1);
		var resources = ResourceBundle.Create(
			(ResourceId.Credits, 10),
			(ResourceId.ScrapAlloy, 20));

		Assert.Same(
			resources,
			StrategicPilotSkillModifiers.ApplySalvageBonus(map, resources));
	}

	[Fact]
	public void ApplySalvageBonus_UpdatesEachLootRollAndRebuildsTotal()
	{
		var map = StarMap.Create(1);
		var grants = StackGrants(EPilotSkillId.DronesYearnForMines, 1);
		var loot = new LootResult(
			[
				new LootRoll(
					"miner-1",
					EType.RepurposedMiner,
					ResourceBundle.Of(ResourceId.ScrapAlloy, 4)),
				new LootRoll(
					"miner-2",
					EType.RepurposedMiner,
					ResourceBundle.Of(ResourceId.ScrapAlloy, 4)),
			],
			ResourceBundle.Of(ResourceId.ScrapAlloy, 8));

		var modified = StrategicPilotSkillModifiers.ApplySalvageBonus(map, loot, grants);

		Assert.All(
			modified.Rolls,
			roll => AssertResource(roll.Awarded, ResourceId.ScrapAlloy, 5));
		AssertResource(modified.Total, ResourceId.ScrapAlloy, 10);
	}

	private static IReadOnlyList<PilotSkillGrant> StackGrants(EPilotSkillId skillId, int stacks)
	{
		var grants = new PilotSkillGrant[stacks];
		for (var index = 0; index < stacks; index++)
			grants[index] = new PilotSkillGrant(skillId, $"pilot-{index}");
		return grants;
	}

	private static void AssertResource(ResourceBundle resources, ResourceId id, int expected)
	{
		Assert.True(resources.TryGet(id, out var actual));
		Assert.Equal(expected, actual);
	}
}
