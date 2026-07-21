using GrimSpace.Battle.Board;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Actions;

public sealed class FlakActionTests
{
	private const string PlayerId = "player";

	[Fact]
	public void FlakSchedulesResolveOnPreviewTimeline()
	{
		var origin = new Coord(5, 5, 5);
		var plan = BeginPlan(origin);
		var flak = new FlakAction(PlayerId, EFlakMount.Port);

		Assert.True(plan.TryApplyAndEnqueue(flak));

		var resolveTick = plan.TurnStartTick + CombatConfig.FlakResolveDelay;
		var scheduled = plan.PreviewTimeline.SnapshotAt(resolveTick);
		Assert.Single(scheduled);
		Assert.IsType<ResolveHazardAction>(scheduled[0]);
		Assert.NotEmpty(plan.Board.TurnHazards);
	}

	[Fact]
	public void AdvanceToTickAppliesFlakMomentumLoss()
	{
		var origin = new Coord(5, 5, 5);
		var plan = BeginPlan(origin, momentum: 1);
		Assert.True(plan.TryApplyAndEnqueue(new FlakAction(PlayerId, EFlakMount.Starboard)));

		var hazard = plan.Board.TurnHazards.First();
		var enemy = plan.Board.Units.Values.First(unit => unit.State.Id != PlayerId);
		enemy.State.Position = hazard.Cells.First();

		plan.AdvanceToTick(plan.TurnStartTick + CombatConfig.FlakResolveDelay);

		Assert.Equal(0, enemy.State.MomentumLevel);
		Assert.True(enemy.State.ApPenaltyNextTurn);
	}

	private static BattleSession BeginPlan(Coord origin, int momentum = 0)
	{
		var player = BattleTestFixture.Player(origin, momentum: momentum);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var plan = new BattleSession();
		plan.BeginTurn(PlayerId, [player, enemy], grid, new Dictionary<string, NonUnit>(), blocked, turnStartTick: 0);
		return plan;
	}
}
