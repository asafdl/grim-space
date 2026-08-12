using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Actions;

namespace GrimSpace.Tests.Player;

public sealed class HumanExecutionAgentTests
{
	private const string PlayerId = "player";

	[Fact]
	public void AcceptedEnqueuePublishesSnapshot()
	{
		var origin = new Coord(5, 5, 5);
		var end = origin + Coord.Forward * 3;
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var agent = battle.PlayerAgent;

		var changes = 0;
		agent.Changed += _ => changes++;

		Assert.True(BattleTestCommands.Move(battle, end));

		Assert.Equal(1, changes);
		Assert.Equal(end, agent.Current.Units[PlayerId].Position);
		Assert.Equal(3, agent.Current.CommittedMovePath.Count);
		Assert.True(agent.Current.CanUndo);
	}

	[Fact]
	public void RejectedEnqueueReturnsFalseWithoutPublishing()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var agent = battle.PlayerAgent;

		var changes = 0;
		agent.Changed += _ => changes++;

		Assert.True(BattleTestCommands.FireRailgun(battle));
		var before = agent.Current;

		Assert.False(BattleTestCommands.FireRailgun(battle));

		Assert.Equal(1, changes);
		Assert.Same(before, agent.Current);
	}

	[Fact]
	public void PlanningDoesNotMutateLiveWorld()
	{
		var origin = new Coord(5, 5, 5);
		var end = origin + Coord.Forward * 3;
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var liveBefore = battle.Engine.World.StateOf(PlayerId).Position;

		Assert.True(BattleTestCommands.Move(battle, end));

		Assert.Equal(liveBefore, battle.Engine.World.StateOf(PlayerId).Position);
		Assert.Equal(end, battle.PlayerAgent.Current.Units[PlayerId].Position);
		Assert.Equal(3, battle.PlayerAgent.Sim.Actions.Count);
	}

	[Fact]
	public void UndoAndCommitFlagsTrackQueueState()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var agent = battle.PlayerAgent;

		Assert.True(agent.Current.CanCommit);
		Assert.False(agent.Current.CanUndo);

		Assert.True(BattleTestCommands.Move(battle, origin + Coord.Forward));

		Assert.True(agent.Current.CanUndo);
		Assert.True(agent.Current.CanCommit);

		Assert.True(BattleTestCommands.Undo(battle));

		Assert.False(agent.Current.CanUndo);
		Assert.True(agent.Current.CanCommit);
		Assert.Empty(agent.Sim.Actions);
	}

	[Fact]
	public void FireRailgunUpdatesQueuedWeaponSnapshot()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var agent = battle.PlayerAgent;

		Assert.False(agent.Current.QueuedWeapon.Railgun);

		Assert.True(BattleTestCommands.FireRailgun(battle));

		Assert.True(agent.Current.QueuedWeapon.Railgun);
		Assert.Equal(0, agent.Current.Units[PlayerId].RailgunRemaining);
		Assert.NotEmpty(agent.Current.ThreatenedUnitIds);
	}

	[Fact]
	public void SnapshotPublishesWeaponAvailabilityWhileCanAct()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var agent = battle.PlayerAgent;

		Assert.True(agent.Current.CanAct);
		Assert.True(agent.Current.Weapons.Railgun);
		Assert.True(agent.Current.Weapons.IsKindLegal(EWeaponKind.Railgun));

		Assert.True(BattleTestCommands.FireRailgun(battle));

		Assert.False(agent.Current.Weapons.Railgun);
	}
}
