using GrimSpace.Battle.Board;
using GrimSpace.Battle.Weapons;
using GrimSpace.Battle.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Actions;

public sealed class FlakActionTests
{
	private const string PlayerId = "player";

	[Fact]
	public void FlakSchedulesResolveOnPreviewTimeline()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginPlanning(origin);
		var flak = new FlakAction(PlayerId, EFlakMount.Port);

		Assert.True(battle.TryEnqueue(flak));

		var resolveTick = battle.Session.AnchorTick + CombatConfig.FlakResolveDelay;
		var scheduled = battle.Board.Timeline.At(resolveTick).Snapshot();
		Assert.Single(scheduled);
		Assert.IsType<ResolveHazardAction>(scheduled[0]);
		Assert.NotEmpty(battle.Board.TurnHazards);
	}

	[Fact]
	public void AdvanceToTickAppliesFlakMomentumLoss()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginPlanning(origin, momentum: 1);
		Assert.True(battle.TryEnqueue(new FlakAction(PlayerId, EFlakMount.Starboard)));

		var hazard = battle.Board.TurnHazards.First();
		var enemy = battle.Board.Units.Values.First(unit => unit.State.Id != PlayerId);
		enemy.State.Position = hazard.Cells.First();

		BattleTestApply.AdvancePreviewToTick(
			battle,
			battle.Session.AnchorTick + CombatConfig.FlakResolveDelay);

		Assert.Equal(0, enemy.State.MomentumLevel);
		Assert.True(enemy.State.ApPenaltyNextTurn);
	}
}
