using GrimSpace.Battle.Objectives;
using GrimSpace.Math;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
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
			[OutcomeTestKit.Handoff("patrol-0", EType.Patrol, 1)]);

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
			[OutcomeTestKit.Handoff("player-ship", EType.Fighter, 0)]);

		var result = LootCatalog.For(outcome);

		Assert.Empty(result.Rolls);
		Assert.True(result.Total.IsEmpty);
	}

	[Fact]
	public void For_DestroyedPatrol_HigherGearTier_IncreasesScrapRange()
	{
		const ulong seed = 42;
		var low = LootCatalog.SalvageFromShip(EType.Patrol, EShipGearTier.T0, new StableRandom(seed));
		var high = LootCatalog.SalvageFromShip(EType.Patrol, EShipGearTier.T3, new StableRandom(seed));

		Assert.True(low.TryGet(ResourceId.ScrapAlloy, out var lowScrap));
		Assert.True(high.TryGet(ResourceId.ScrapAlloy, out var highScrap));
		Assert.InRange(lowScrap, 50, 120);
		Assert.InRange(highScrap, 72, 174);
		Assert.True(highScrap > lowScrap);
	}

	[Fact]
	public void SalvageFromShip_PowerAbove10_CanDropIndustrialCore()
	{
		var carrier = new ShipPowerLevel(EType.Carrier, EShipGearTier.T0, 11);
		var foundCore = false;
		for (ulong seed = 0; seed < 500; seed++)
		{
			var loot = LootCatalog.SalvageFromShip(carrier, new StableRandom(seed));
			if (loot.TryGet(ResourceId.IndustrialCore, out _))
				foundCore = true;
		}

		Assert.True(foundCore);
	}

	[Fact]
	public void SalvageFromShip_PowerAtMost10_NeverDropsIndustrialCore()
	{
		var patrol = new ShipPowerLevel(EType.Patrol, EShipGearTier.T3, 8);
		for (ulong seed = 0; seed < 200; seed++)
		{
			var loot = LootCatalog.SalvageFromShip(patrol, new StableRandom(seed));
			Assert.False(loot.TryGet(ResourceId.IndustrialCore, out _));
		}
	}

	[Fact]
	public void For_DestroyedTorpedo_YieldsEmptyLoot()
	{
		var outcome = new BattleOutcome(
			"test-battle",
			EBattleResult.Win,
			[OutcomeTestKit.Handoff("torpedo-1", EType.Torpedo, 0)]);

		var result = LootCatalog.For(outcome);

		Assert.Empty(result.Rolls);
		Assert.True(result.Total.IsEmpty);
	}

	private static BattleOutcome OutcomeWithDestroyedPatrols(params string[] patrolIds) =>
		new(
			"test-battle",
			EBattleResult.Win,
			[.. patrolIds.Select(id => OutcomeTestKit.Handoff(id, EType.Patrol, 0))]);
}
