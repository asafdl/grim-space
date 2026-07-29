using GrimSpace.Battle;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Weapons;
using GrimSpace.Battle.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Actions;

public sealed class FlakActionTests
{
	private const string PlayerId = "player";

	private static int TotalShieldPoints(GrimSpace.Battle.Units.State state)
	{
		var total = 0;
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			total += state.ShieldPoints[face];
		return total;
	}

	[Fact]
	public void FlakSchedulesResolveOnPreviewTimeline()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var flak = new FlakAction(PlayerId, EFlakMount.Port);

		Assert.Equal(CombatConfig.FlaksPerTurn, battle.Sim.StateOf<ActorState>(PlayerId).FlakRemaining);
		Assert.True(battle.Sim.TryEnqueue(flak));
		Assert.Equal(CombatConfig.FlaksPerTurn - 1, battle.Sim.StateOf<ActorState>(PlayerId).FlakRemaining);
		Assert.False(battle.Sim.TryEnqueue(new FlakAction(PlayerId, EFlakMount.Starboard)));

		var resolveTick = battle.Sim.AnchorTick + CombatConfig.FlakResolveDelay;
		var scheduled = battle.Sim.PeekTimeline(resolveTick);
		var hazard = Assert.Single(scheduled.OfType<ResolveHazardAction>());
		Assert.NotEmpty(hazard.Cells);
		Assert.Equal(CombatConfig.FlakDamage, hazard.Damage);
	}

	[Fact]
	public void AdvanceToTickAppliesFlakMomentumLoss()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin, momentum: 1);
		Assert.True(battle.Sim.TryEnqueue(new FlakAction(PlayerId, EFlakMount.Starboard)));

		var resolveTick = battle.Sim.AnchorTick + CombatConfig.FlakResolveDelay;
		var hazard = battle.Sim.PeekTimeline(resolveTick).OfType<ResolveHazardAction>().First();
		var enemy = battle.Sim.World.Units.Values.First(unit => unit.State.Id != PlayerId);
		enemy.State.Position = hazard.Cells.First();
		var shieldsBefore = TotalShieldPoints(enemy.State);

		BattleTestApply.AdvancePreviewToTick(battle, resolveTick);

		Assert.Equal(shieldsBefore - CombatConfig.FlakDamage, TotalShieldPoints(enemy.State));
		Assert.Equal(0, enemy.State.MomentumLevel);
		Assert.True(enemy.State.ApPenaltyNextTurn);
	}
}
