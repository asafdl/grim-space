using GrimSpace.Battle.Objectives;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.PilotCapabilities;

namespace GrimSpace.Tests.World.StarSystem.PilotSkills;

public sealed class ScrapRewardBonusModifierTests
{
	[Fact]
	public void Modify_EngagementLoot_UpdatesRollsAndRebuildsTotal()
	{
		var action = new ResolveEngagementAction(
			"player",
			new BattleOutcome("battle", EBattleResult.Win, []),
			[],
			ResourceBundle.Empty);
		var effect = new AwardEngagementLootEffect(
			[
				new LootRoll(
					"miner-1",
					EType.RepurposedMiner,
					ResourceBundle.Of(ResourceId.ScrapAlloy, 4)),
				new LootRoll(
					"miner-2",
					EType.RepurposedMiner,
					ResourceBundle.Create(
						(ResourceId.ScrapAlloy, 4),
						(ResourceId.IndustrialCore, 1))),
			],
			ResourceBundle.Create(
				(ResourceId.ScrapAlloy, 8),
				(ResourceId.IndustrialCore, 1)));

		var modified = Assert.IsType<AwardEngagementLootEffect>(
			new ScrapRewardBonusModifier(1).Modify(
				action,
				StarMap.Create(1),
				new ActorRuntime(),
				effect));

		Assert.All(modified.Rolls, roll => AssertResource(roll.Awarded, ResourceId.ScrapAlloy, 5));
		AssertResource(modified.Total, ResourceId.ScrapAlloy, 10);
		AssertResource(modified.Total, ResourceId.IndustrialCore, 1);
	}

	[Fact]
	public void AwardEngagementLootEffect_UndoRestoresResources()
	{
		var map = StarMap.Create(1);
		var runtime = new ActorRuntime();
		var effect = new AwardEngagementLootEffect(
			[new LootRoll(
				"miner-1",
				EType.RepurposedMiner,
				ResourceBundle.Of(ResourceId.ScrapAlloy, 5))],
			ResourceBundle.Of(ResourceId.ScrapAlloy, 5));

		effect.Apply(map, runtime, "player");
		Assert.Equal(5, map.PlayerResources.GetBalance(ResourceId.ScrapAlloy));

		effect.Undo(map, runtime, "player");
		Assert.Equal(0, map.PlayerResources.GetBalance(ResourceId.ScrapAlloy));
	}

	private static void AssertResource(ResourceBundle resources, ResourceId id, int expected)
	{
		Assert.True(resources.TryGet(id, out var actual));
		Assert.Equal(expected, actual);
	}
}
