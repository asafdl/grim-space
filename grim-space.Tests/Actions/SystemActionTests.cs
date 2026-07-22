using GrimSpace.Battle;
using GrimSpace.Battle.Board;
using GrimSpace.Core;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Weapons;
using GrimSpace.Battle.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Actions;

public sealed class SystemActionTests
{
	private const string PlayerId = "player";

	[Fact]
	public void ExecuteTurnClearsLeftoverTurnHazardsAtEndTick()
	{
		var battle = TurnOrchestrationTests.CreateOrchestrator(new Coord(5, 5, 5), new Coord(0, 0, 0));
		var hazard = Hazard.FlakBurst(
			"flak-leftover",
			PlayerId,
			BodyFrame.WorldAligned(new Coord(1, 1, 1)),
			[new Coord(1, 1, 1)]);
		battle.Hazards.MutableNonUnits[hazard.Id] = hazard;
		Assert.Single(battle.Hazards.Active);

		Assert.True(battle.ResolveTurn([]));

		Assert.Empty(battle.Hazards.Active);
	}

	[Fact]
	public void ExecuteTurnClearsTurnHazardsViaSystemAction()
	{
		var battle = TurnOrchestrationTests.CreateOrchestrator(new Coord(5, 5, 5), new Coord(0, 0, 0));
		Assert.True(battle.TryEnqueue(new FlakAction(PlayerId, EFlakMount.Port)));

		Assert.True(battle.ResolveTurn(battle.Actions.ToList()));
		Assert.Empty(battle.Hazards.Active);
	}

	[Fact]
	public void ExecuteTurnPreservesBoardHazards()
	{
		var battle = TurnOrchestrationTests.CreateOrchestrator(new Coord(5, 5, 5), new Coord(0, 0, 0));
		var asteroid = Hazard.Asteroid(
			"asteroid-1",
			new Coord(2, 2, 2),
			battle.Grid,
			radius: 1,
			visualId: "rock");
		battle.Hazards.MutableNonUnits[asteroid.Id] = asteroid;

		Assert.True(battle.ResolveTurn([]));

		Assert.Contains(asteroid.Id, battle.Hazards.NonUnits.Keys);
		Assert.Equal(EntityIds.World, battle.Hazards.NonUnits[asteroid.Id].OwnerId);
	}

	[Fact]
	public void ResolveHazardActionRemovesHazardAfterApplying()
	{
		var battle = BattleTestFixture.BeginPlanning(new Coord(5, 5, 5));
		Assert.True(battle.TryEnqueue(new FlakAction(PlayerId, EFlakMount.Starboard)));
		Assert.NotEmpty(battle.Board.TurnHazards);

		BattleTestApply.AdvancePreviewToTick(
			battle,
			battle.Session.AnchorTick + CombatConfig.FlakResolveDelay);

		Assert.Empty(battle.Board.TurnHazards);
	}
}
