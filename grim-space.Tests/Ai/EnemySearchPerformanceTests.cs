using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Dfs;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Ai;

[BattleTestSuite]
public sealed class EnemySearchPerformanceTests
{
	[Fact]
	public void DevGooper_SearchStaysWithinFrameBudget()
	{
		const int seed = 2_122_565_999;
		var battle = BattleOrchestrator.FromEncounter(
			BattleEncounter.DevDefault(seed),
			gridSize: 64);
		var gooper = UnitRegistry.For(battle.Engine.World)
			.All.Single(unit => unit.State.Type == EType.IndustrialGooper);
		var frames = ActionSearch.Run(
				battle.Engine.CreateSimulation(),
				gooper.State.Id,
				AiController.SearchCapabilities(gooper.State),
				new SearchInput<BattleWorld, ActorRuntime>(
					BattleSearchVisit.ForCapabilities,
					ShouldExploreAction: EnemySearchInput.ShouldExploreAction))
			.Count();

		Assert.InRange(frames, 1, 12_000);
	}

	[Fact]
	public void IndustrialGooper_SearchOmitsRotationallyEquivalentRolls()
	{
		var battle = BattleOrchestrator.FromEncounter(BattleEncounter.DevDefault());
		var gooper = UnitRegistry.For(battle.Engine.World)
			.All.Single(unit => unit.State.Type == EType.IndustrialGooper);

		Assert.DoesNotContain(
			AiController.SearchCapabilities(gooper.State),
			definition => definition is RollDef);
	}
}
