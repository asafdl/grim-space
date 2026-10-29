using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Heat;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class HeatScheduleTransitionTests
{
	private const string PlayerId = "player";

	[Fact]
	public void TurnUpkeep_CoolsByClearPerTurnWhenNotOverdriveOrOverheat()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Heat = 55;

		battle.Engine.Commit(new TurnUpkeepAction(PlayerId));

		Assert.Equal(35, state.Heat);
		Assert.False(state.Overdrive);
		Assert.False(state.Overheat);
	}

	[Fact]
	public void TurnUpkeep_DoesNotCoolDuringOverdrive()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Heat = 100;
		state.Overdrive = true;

		battle.Engine.Commit(new TurnUpkeepAction(PlayerId));

		Assert.Equal(100, state.Heat);
		Assert.True(state.Overdrive);
	}

	[Fact]
	public void TurnUpkeep_DoesNotCoolDuringOverheat()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Heat = 100;
		state.Overheat = true;

		battle.Engine.Commit(new TurnUpkeepAction(PlayerId));

		Assert.Equal(100, state.Heat);
		Assert.True(state.Overheat);
	}

	[Fact]
	public void CrossingCapacity_SchedulesOverheatThenClearAcrossTicks()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Heat = 90;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);

		new HeatChangeEffect(20).Apply(battle.Engine.World, runtime, PlayerId);

		Assert.Equal(100, state.Heat);
		Assert.True(state.Overdrive);
		Assert.False(state.Overheat);
		Assert.True(battle.Engine.World.Timeline.ContainsPending(
			action => action is EnterOverheatAction enter && enter.ActorId == PlayerId));

		battle.Engine.Commit(new TurnUpkeepAction(PlayerId));
		Assert.Equal(100, state.Heat);
		Assert.True(state.Overdrive);

		battle.Engine.AdvanceTick();

		Assert.Equal(100, state.Heat);
		Assert.False(state.Overdrive);
		Assert.True(state.Overheat);
		Assert.True(battle.Engine.World.Timeline.ContainsPending(
			action => action is ClearOverheatAction clear && clear.ActorId == PlayerId));

		battle.Engine.Commit(new TurnUpkeepAction(PlayerId));
		Assert.Equal(100, state.Heat);
		Assert.True(state.Overheat);

		battle.Engine.AdvanceTick();

		Assert.Equal(0, state.Heat);
		Assert.False(state.Overdrive);
		Assert.False(state.Overheat);
	}

	[Fact]
	public void MoveDuringOverheatLeavesHeatUnchanged()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Heat = 100;
		state.Overheat = true;

		Assert.True(BattleTestApply.TryApplyOne(
			MoveDef.Instance.Bind(PlayerId, ESpatialOrientation.Forward),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId));

		Assert.Equal(100, state.Heat);
		Assert.False(state.Overdrive);
		Assert.True(state.Overheat);
	}

	[Fact]
	public void TurnUpkeep_WithoutHeatProfileDoesNotCool()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.HeatProfile = HeatSpec.None;
		state.Heat = 40;

		battle.Engine.Commit(new TurnUpkeepAction(PlayerId));

		Assert.Equal(40, state.Heat);
	}

}
