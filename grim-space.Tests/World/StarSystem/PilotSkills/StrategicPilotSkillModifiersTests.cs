using GrimSpace.Pilots;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.PilotSkills;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.Run;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.World.StarSystem.PilotSkills;

public sealed class StrategicPilotSkillModifiersTests
{
	[Fact]
	public void ApplySalvageBonus_StacksWithoutChangingOtherResources()
	{
		var map = StarMap.Create(
			1,
			pilotSkillRosterReader: new SkillRoster(dronesYearnForMines: 2));
		var resources = ResourceBundle.Create(
			(ResourceId.ScrapAlloy, 101),
			(ResourceId.IndustrialCore, 1));

		var modified = StrategicPilotSkillModifiers.ApplySalvageBonus(map, resources);

		AssertResource(modified, ResourceId.ScrapAlloy, 131);
		AssertResource(modified, ResourceId.IndustrialCore, 1);
	}

	[Fact]
	public void ApplyUpgradeDiscount_StacksPastThirtyPercentAndRoundsCostsUp()
	{
		var map = StarMap.Create(
			1,
			pilotSkillRosterReader: new SkillRoster(iKnowABot: 4));
		var cost = ResourceBundle.Create(
			(ResourceId.Credits, 101),
			(ResourceId.ScrapAlloy, 11),
			(ResourceId.IndustrialCore, 2));

		var modified = StrategicPilotSkillModifiers.ApplyUpgradeDiscount(map, cost);

		AssertResource(modified, ResourceId.Credits, 61);
		AssertResource(modified, ResourceId.ScrapAlloy, 7);
		AssertResource(modified, ResourceId.IndustrialCore, 2);
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
		Assert.Same(
			resources,
			StrategicPilotSkillModifiers.ApplyUpgradeDiscount(map, resources));
	}

	[Fact]
	public void ApplySalvageBonus_UpdatesEachLootRollAndRebuildsTotal()
	{
		var map = StarMap.Create(
			1,
			pilotSkillRosterReader: new SkillRoster(dronesYearnForMines: 1));
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

		var modified = StrategicPilotSkillModifiers.ApplySalvageBonus(map, loot);

		Assert.All(
			modified.Rolls,
			roll => AssertResource(roll.Awarded, ResourceId.ScrapAlloy, 5));
		AssertResource(modified.Total, ResourceId.ScrapAlloy, 10);
	}

	private static void AssertResource(ResourceBundle resources, ResourceId id, int expected)
	{
		Assert.True(resources.TryGet(id, out var actual));
		Assert.Equal(expected, actual);
	}

	private sealed class SkillRoster(
		int dronesYearnForMines = 0,
		int iKnowABot = 0) : IPilotSkillRosterReader
	{
		public int LearnedCount(EPilotSkillId skillId) =>
			skillId switch
			{
				EPilotSkillId.DronesYearnForMines => dronesYearnForMines,
				EPilotSkillId.IKnowABot => iKnowABot,
				_ => 0,
			};
	}
}
