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
		var result = LootCatalog.For(OutcomeWithDestroyedPatrols("pirate-fleet", "patrol-0"));
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
		var result = LootCatalog.For(OutcomeWithDestroyedPatrols("pirate-fleet", "patrol-0", "patrol-1", "patrol-2"));

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
		var outcome = BattleOutcome.Create(
			EBattleResult.Ongoing,
			[("player-fleet", EBattleParticipantState.Alive), ("pirate-fleet", EBattleParticipantState.Alive)],
			[new TacticalUnitOutcome("patrol-0", "pirate-fleet", EType.Patrol, EBattleParticipantState.Alive)]);

		var result = LootCatalog.For(outcome);

		Assert.Empty(result.Rolls);
		Assert.True(result.Total.IsEmpty);
	}

	[Fact]
	public void For_PlayerCasualty_YieldsEmptyLoot()
	{
		var outcome = BattleOutcome.Create(
			EBattleResult.Ongoing,
			[("player-fleet", EBattleParticipantState.Alive), ("pirate-fleet", EBattleParticipantState.Alive)],
			[new TacticalUnitOutcome("player-ship", "player-fleet", EType.Fighter, EBattleParticipantState.Destroyed)]);

		var result = LootCatalog.For(outcome);

		Assert.Empty(result.Rolls);
		Assert.True(result.Total.IsEmpty);
	}

	[Fact]
	public void For_DestroyedTorpedo_YieldsEmptyLoot()
	{
		var outcome = BattleOutcome.Create(
			EBattleResult.Win,
			[("player-fleet", EBattleParticipantState.Alive), ("pirate-fleet", EBattleParticipantState.Destroyed)],
			[new TacticalUnitOutcome("torpedo-1", "pirate-fleet", EType.Torpedo, EBattleParticipantState.Destroyed)]);

		var result = LootCatalog.For(outcome);

		Assert.Empty(result.Rolls);
		Assert.True(result.Total.IsEmpty);
	}

	[Fact]
	public void For_DestroyedPatrolOnAliveFleet_YieldsEmptyLoot()
	{
		var outcome = BattleOutcome.Create(
			EBattleResult.Ongoing,
			[("player-fleet", EBattleParticipantState.Alive), ("pirate-fleet", EBattleParticipantState.Alive)],
			[new TacticalUnitOutcome("patrol-0", "pirate-fleet", EType.Patrol, EBattleParticipantState.Destroyed)]);

		var result = LootCatalog.For(outcome);

		Assert.Empty(result.Rolls);
		Assert.True(result.Total.IsEmpty);
	}

	private static BattleOutcome OutcomeWithDestroyedPatrols(string fleetId, params string[] patrolIds) =>
		BattleOutcome.Create(
			EBattleResult.Win,
			[("player-fleet", EBattleParticipantState.Alive), (fleetId, EBattleParticipantState.Destroyed)],
			[.. patrolIds.Select(id => new TacticalUnitOutcome(
				id,
				fleetId,
				EType.Patrol,
				EBattleParticipantState.Destroyed))]);
}
