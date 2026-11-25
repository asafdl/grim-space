using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Actions;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Units.Enums;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.PilotCapabilities;

namespace GrimSpace.Tests.World.StarSystem.PilotSkills;

public sealed class ScrapRewardBonusRuleTests
{
	[Fact]
	public void EngagementLoot_UpdatesRollsAndRebuildsTotal()
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
			Prepare(action, effect, EPilotCapabilityId.ScrapRewardBonus));

		Assert.All(modified.Rolls, roll => AssertResource(roll.Awarded, ResourceId.ScrapAlloy, 5));
		AssertResource(modified.Total, ResourceId.ScrapAlloy, 10);
		AssertResource(modified.Total, ResourceId.IndustrialCore, 1);
	}

	[Fact]
	public void MinorAndStandardBonusesAdd()
	{
		var effect = new ChangeResourceEffect("test", ResourceBundle.Of(ResourceId.ScrapAlloy, 100));

		var modified = Assert.IsType<ChangeResourceEffect>(Prepare(
			new InvestigateWreckageAction("player", "contract"),
			effect,
			EPilotCapabilityId.MinorScrapRewardBonus,
			EPilotCapabilityId.ScrapRewardBonus));

		AssertResource(
			modified.Change,
			ResourceId.ScrapAlloy,
			100 + ScrapRewardBonusRule.MinorBonusPercent + ScrapRewardBonusRule.StandardBonusPercent);
	}

	[Fact]
	public void ContractCompletion_IsNotSalvageAndKeepsScrap()
	{
		var effect = new ChangeResourceEffect("test", ResourceBundle.Of(ResourceId.ScrapAlloy, 100));

		var result = Prepare(
			new CompleteContractAction("player", "contract", EFaction.Pirates, ResourceBundle.Empty),
			effect,
			EPilotCapabilityId.ScrapRewardBonus);

		Assert.Same(effect, result);
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
		Assert.Equal(
			StrategicEconomyConfig.StartingScrapAlloy + 5,
			map.PlayerResources.GetBalance(ResourceId.ScrapAlloy));

		effect.Undo(map, runtime, "player");
		Assert.Equal(
			StrategicEconomyConfig.StartingScrapAlloy,
			map.PlayerResources.GetBalance(ResourceId.ScrapAlloy));
	}

	private static IEffect<StarMap, ActorRuntime> Prepare(
		IAction action,
		IEffect<StarMap, ActorRuntime> effect,
		params EPilotCapabilityId[] capabilities)
	{
		var runtime = new ActorRuntime();
		runtime.ReplaceStrategicCapabilities(capabilities
			.Select(id => new PilotCapabilityGrant(
				id,
				"pilot",
				new PilotCapabilitySource(EPilotCapabilitySourceKind.Skill, "test-skill")))
			.ToArray());
		return StarMapPilotCapabilities.Rules.Prepare(action, StarMap.Create(1), runtime, effect);
	}

	private static void AssertResource(ResourceBundle resources, ResourceId id, int expected)
	{
		Assert.True(resources.TryGet(id, out var actual));
		Assert.Equal(expected, actual);
	}
}
