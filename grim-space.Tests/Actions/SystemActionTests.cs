using GrimSpace.Battle;
using GrimSpace.Battle.Board;
using GrimSpace.Core;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Actions;

public sealed class SystemActionTests
{
	private const string PlayerId = "player";

	[Fact]
	public void ExecuteTurnClearsLeftoverTurnHazardsAtEndTick()
	{
		var manager = TurnOrchestrationTests.CreateManager(new Coord(5, 5, 5), new Coord(0, 0, 0));
		var hazard = Hazard.FlakBurst(
			"flak-leftover",
			PlayerId,
			BodyFrame.WorldAligned(new Coord(1, 1, 1)),
			[new Coord(1, 1, 1)]);
		manager.Hazards.MutableNonUnits[hazard.Id] = hazard;
		Assert.Single(manager.Hazards.Active);

		Assert.True(manager.ExecuteTurn(manager.Player.FinalizePlan()));

		Assert.Empty(manager.Hazards.Active);
	}

	[Fact]
	public void ExecuteTurnClearsTurnHazardsViaSystemAction()
	{
		var manager = TurnOrchestrationTests.CreateManager(new Coord(5, 5, 5), new Coord(0, 0, 0));
		manager.Player.BeginTurn(0);
		Assert.True(manager.Player.TryEnqueue(new FlakAction(PlayerId, EFlakMount.Port)));

		Assert.True(manager.ExecuteTurn(manager.Player.FinalizePlan()));
		Assert.Empty(manager.Hazards.Active);
	}

	[Fact]
	public void ExecuteTurnPreservesBoardHazards()
	{
		var manager = TurnOrchestrationTests.CreateManager(new Coord(5, 5, 5), new Coord(0, 0, 0));
		var asteroid = Hazard.Asteroid(
			"asteroid-1",
			new Coord(2, 2, 2),
			manager.Grid,
			radius: 1,
			visualId: "rock");
		manager.Hazards.MutableNonUnits[asteroid.Id] = asteroid;

		Assert.True(manager.ExecuteTurn(manager.Player.FinalizePlan()));

		Assert.Contains(asteroid.Id, manager.Hazards.NonUnits.Keys);
		Assert.Equal(EntityIds.World, manager.Hazards.NonUnits[asteroid.Id].OwnerId);
	}

	[Fact]
	public void ResolveHazardActionRemovesHazardAfterApplying()
	{
		var plan = BeginPlan(new Coord(5, 5, 5));
		Assert.True(plan.TryApplyAndEnqueue(new FlakAction(PlayerId, EFlakMount.Starboard)));
		Assert.NotEmpty(plan.Board.TurnHazards);

		plan.AdvanceToTick(plan.TurnStartTick + CombatConfig.FlakResolveDelay);

		Assert.Empty(plan.Board.TurnHazards);
	}

	private static BattleSession BeginPlan(Coord origin)
	{
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var plan = new BattleSession();
		plan.BeginTurn(
			PlayerId,
			[player, enemy],
			BattleTestFixture.Grid(),
			new Dictionary<string, GrimSpace.Battle.Board.NonUnit>(),
			new HashSet<Coord> { enemy.State.Position },
			turnStartTick: 0);
		return plan;
	}
}
