using GrimSpace.Battle;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Units;

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
	public void FlakAppliesResolveImmediately()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var flak = new FlakAction(PlayerId, EFlakMount.Port);

		Assert.Equal(CombatConfig.FlaksPerTurn, battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId).FlakRemaining);
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(flak));
		Assert.Equal(CombatConfig.FlaksPerTurn - 1, battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId).FlakRemaining);
		Assert.False(battle.PlayerAgent.Sim.TryEnqueue(new FlakAction(PlayerId, EFlakMount.Starboard)));
	}

	[Fact]
	public void FlakAppliesMomentumLossOnEnqueue()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin, momentum: 1);
		var frame = BodyFrame.From(battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId));
		var cells = WeaponBursts.FlakBurstCells(
			frame,
			FlakMountConfig.For(EFlakMount.Starboard),
			battle.PlayerAgent.Sim.World.Grid.IsInBounds);
		var enemy = UnitRegistry.For(battle.PlayerAgent.Sim.World).All.First(unit => unit.State.Id != PlayerId);
		enemy.State.Position = cells.First();
		var shieldsBefore = TotalShieldPoints(enemy.State);

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new FlakAction(PlayerId, EFlakMount.Starboard)));

		Assert.Equal(shieldsBefore - CombatConfig.FlakDamage, TotalShieldPoints(enemy.State));
		Assert.Equal(0, enemy.State.MomentumLevel);
		Assert.True(enemy.State.ApPenaltyNextTurn);
	}
}
