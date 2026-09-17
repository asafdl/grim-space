using GrimSpace.Battle.Objectives;
using GrimSpace.Run;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Tests.Run;

public sealed class LootCatalogTests
{
	[Fact]
	public void For_DestroyedPatrolFromDefeatedFleet_GrantsScrapInRange()
	{
		var result = LootCatalog.For(OutcomeWithDestroyedPatrols("patrol-0"));
		var roll = Assert.Single(result.Rolls);

		Assert.Equal("patrol-0", roll.TacticalUnitId);
		Assert.Equal(EType.Patrol, roll.Kind);
		Assert.True(roll.Awarded.TryGet(ResourceId.ScrapAlloy, out var scrap));
		Assert.InRange(scrap, 50, 120);
		Assert.True(result.Total.TryGet(ResourceId.ScrapAlloy, out var total));
		Assert.Equal(scrap, total);
	}

	[Fact]
	public void For_MultipleDestroyedPatrols_SumsRollsInRange()
	{
		var result = LootCatalog.For(OutcomeWithDestroyedPatrols("patrol-0", "patrol-1", "patrol-2"));

		Assert.Equal(3, result.Rolls.Count);
		var expectedTotal = 0;
		foreach (var roll in result.Rolls)
		{
			Assert.True(roll.Awarded.TryGet(ResourceId.ScrapAlloy, out var scrap));
			Assert.InRange(scrap, 50, 120);
			expectedTotal += scrap;
		}

		Assert.True(result.Total.TryGet(ResourceId.ScrapAlloy, out var total));
		Assert.Equal(expectedTotal, total);
	}

	[Fact]
	public void For_SurvivingPatrol_YieldsEmptyLoot()
	{
		var outcome = new BattleOutcome(
			"test-battle",
			EBattleResult.Ongoing,
			[new UnitStateHandoff(1, EType.Patrol, "patrol-0")]);

		var result = LootCatalog.For(outcome);

		Assert.Empty(result.Rolls);
		Assert.True(result.Total.IsEmpty);
	}

	[Fact]
	public void For_PlayerCasualty_YieldsEmptyLoot()
	{
		var outcome = new BattleOutcome(
			"test-battle",
			EBattleResult.Ongoing,
			[new UnitStateHandoff(0, EType.Fighter, "player-ship")]);

		var result = LootCatalog.For(outcome);

		Assert.Empty(result.Rolls);
		Assert.True(result.Total.IsEmpty);
	}

	[Fact]
	public void For_DestroyedTorpedo_YieldsEmptyLoot()
	{
		var outcome = new BattleOutcome(
			"test-battle",
			EBattleResult.Win,
			[new UnitStateHandoff(0, EType.Torpedo, "torpedo-1")]);

		var result = LootCatalog.For(outcome);

		Assert.Empty(result.Rolls);
		Assert.True(result.Total.IsEmpty);
	}

	private static BattleOutcome OutcomeWithDestroyedPatrols(params string[] patrolIds) =>
		new(
			"test-battle",
			EBattleResult.Win,
			[.. patrolIds.Select(id => new UnitStateHandoff(
				0,
				EType.Patrol,
				id))]);
}
