using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Actions;

namespace GrimSpace.Tests.Player;

public sealed class HumanExecutionAgentTests
{
	private const string PlayerId = "player";

	[Fact]
	public void AcceptedEnqueueNotifiesPlanningChanged()
	{
		var origin = new Coord(5, 5, 5);
		var end = origin + Coord.Forward * 3;
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var agent = battle.PlayerAgent;
		var preview = new PlanningPreview();

		var changes = 0;
		agent.PlanningChanged += () => changes++;

		Assert.True(BattleTestCommands.Move(battle, end));

		Assert.Equal(1, changes);
		Assert.Equal(end, preview.PreviewUnits(agent.Sim, PlayerId)[PlayerId].Position);
		Assert.Equal(3, preview.CommittedMovePath(agent.Sim, PlayerId).Count);
		Assert.True(agent.CanUndo);
	}

	[Fact]
	public void RejectedEnqueueReturnsFalseWithoutNotifying()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var agent = battle.PlayerAgent;

		var changes = 0;
		agent.PlanningChanged += () => changes++;

		Assert.True(BattleTestCommands.FireRailgun(battle));
		var actionsBefore = agent.Sim.Actions.Count;

		Assert.False(BattleTestCommands.FireRailgun(battle));

		Assert.Equal(1, changes);
		Assert.Equal(actionsBefore, agent.Sim.Actions.Count);
	}

	[Fact]
	public void PlanningDoesNotMutateLiveWorld()
	{
		var origin = new Coord(5, 5, 5);
		var end = origin + Coord.Forward * 3;
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var preview = new PlanningPreview();
		var liveBefore = battle.Engine.World.StateOf(PlayerId).Position;

		Assert.True(BattleTestCommands.Move(battle, end));

		Assert.Equal(liveBefore, battle.Engine.World.StateOf(PlayerId).Position);
		Assert.Equal(end, preview.PreviewUnits(battle.PlayerAgent.Sim, PlayerId)[PlayerId].Position);
		Assert.Equal(3, battle.PlayerAgent.Sim.Actions.Count);
	}

	[Fact]
	public void UndoAndPlanningFlagsTrackQueueState()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var agent = battle.PlayerAgent;

		Assert.True(agent.IsPlanning);
		Assert.False(agent.CanUndo);

		Assert.True(BattleTestCommands.Move(battle, origin + Coord.Forward));

		Assert.True(agent.CanUndo);
		Assert.True(agent.IsPlanning);

		Assert.True(BattleTestCommands.Undo(battle));

		Assert.False(agent.CanUndo);
		Assert.True(agent.IsPlanning);
		Assert.Empty(agent.Sim.Actions);
	}

	[Fact]
	public void FireRailgunUpdatesQueuedWeaponPreview()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var agent = battle.PlayerAgent;
		var preview = new PlanningPreview();

		Assert.False(preview.QueuedWeapon(agent.Sim, PlayerId).Railgun);

		Assert.True(BattleTestCommands.FireRailgun(battle));

		Assert.True(preview.QueuedWeapon(agent.Sim, PlayerId).Railgun);
		Assert.Equal(0, preview.PreviewUnits(agent.Sim, PlayerId)[PlayerId].RailgunRemaining);
		Assert.NotEmpty(preview.ThreatenedUnitIds(agent.Sim, PlayerId, new InteractionState()));
	}

	[Fact]
	public void PlanningExposesWeaponAvailabilityWhileActive()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var agent = battle.PlayerAgent;
		var preview = new PlanningPreview();

		Assert.True(agent.IsPlanning);
		Assert.True(preview.Weapons(agent.Sim, PlayerId).Railgun);
		Assert.True(preview.Weapons(agent.Sim, PlayerId).IsKindLegal(EWeaponKind.Railgun));

		Assert.True(BattleTestCommands.FireRailgun(battle));

		Assert.False(preview.Weapons(agent.Sim, PlayerId).Railgun);
	}

	[Fact]
	public void OpenTurnNotifiesPlanningChangedBeforeGetActionsAsync()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var agent = battle.PlayerAgent;

		battle.SetActive(null);

		var changes = 0;
		agent.PlanningChanged += () => changes++;

		battle.SetActive(PlayerId);

		Assert.Equal(1, changes);
		Assert.True(agent.IsPlanning);
		Assert.True(BattleTestCommands.Move(battle, origin + Coord.Forward));
	}

	[Fact]
	public async Task GetActionsAsyncBlocksUntilCommit()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var agent = battle.PlayerAgent;

		battle.SetActive(null);
		battle.SetActive(PlayerId);
		Assert.True(BattleTestCommands.Move(battle, origin + Coord.Forward));

		var actionsTask = agent.GetActions();
		Assert.False(actionsTask.IsCompleted);

		Assert.True(agent.Commit());

		var actions = await actionsTask;
		Assert.Single(actions, action => action is MoveStepAction);
	}

	[Fact]
	public void GetActionsAsyncDoesNotForkSim()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var agent = battle.PlayerAgent;

		battle.SetActive(null);
		battle.SetActive(PlayerId);
		var simAtOpen = agent.Sim;
		Assert.True(BattleTestCommands.Move(battle, origin + Coord.Forward));
		Assert.True(agent.Commit());

		var actions = agent.GetActions().GetAwaiter().GetResult();
		Assert.Same(simAtOpen, agent.Sim);
		Assert.Single(actions, action => action is MoveStepAction);
	}
}
