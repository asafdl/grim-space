using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Heat;

namespace GrimSpace.Tests.Effects;

[BattleTestSuite]
public sealed class HeatChangeEffectTests
{
	private const string PlayerId = "player";

	[Fact]
	public void ApplyAddsHeatAndUndoRestoresHeatAndOverdriveAndCancelsScheduledOverheat()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Heat = 90;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		var effect = new HeatChangeEffect(20);

		effect.Apply(battle.Engine.World, runtime, PlayerId);

		Assert.Equal(100, state.Heat);
		Assert.True(state.Overdrive);
		Assert.True(battle.Engine.World.Timeline.ContainsPending(
			action => action is EnterOverheatAction enter && enter.ActorId == PlayerId));

		effect.Undo(battle.Engine.World, runtime, PlayerId);

		Assert.Equal(90, state.Heat);
		Assert.False(state.Overdrive);
		Assert.False(battle.Engine.World.Timeline.ContainsPending(
			action => action is EnterOverheatAction enter && enter.ActorId == PlayerId));
	}

	[Fact]
	public void ApplyCapsAtCapacityAndDiscardsOverflow()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Heat = 95;
		state.Overdrive = true;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);

		new HeatChangeEffect(50).Apply(battle.Engine.World, runtime, PlayerId);

		Assert.Equal(100, state.Heat);
		Assert.True(state.Overdrive);
	}

	[Fact]
	public void ApplyIsNoOpWhenOverheat()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Heat = 100;
		state.Overheat = true;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);

		new HeatChangeEffect(20).Apply(battle.Engine.World, runtime, PlayerId);

		Assert.Equal(100, state.Heat);
		Assert.False(state.Overdrive);
	}

	[Fact]
	public void ApplyIsNoOpWhenProfileDoesNotParticipate()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.HeatProfile = HeatSpec.None;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);

		new HeatChangeEffect(20).Apply(battle.Engine.World, runtime, PlayerId);

		Assert.Equal(0, state.Heat);
		Assert.False(state.Overdrive);
	}

	[Fact]
	public void ApplyScalesPositiveGainByProfileMultiplier()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.HeatProfile = new HeatSpec(100, 20, 2f);
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);

		new HeatChangeEffect(HeatChangeEffect.PerApSpent).Apply(battle.Engine.World, runtime, PlayerId);

		Assert.Equal(HeatChangeEffect.PerApSpent * 2, state.Heat);
	}
}
