using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Runtime;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Actions;

namespace GrimSpace.Tests.Presentation;

public sealed class AbilityInstructionFrameTests
{
	[Fact]
	public void RailgunModeShowsPassiveMountInstruction()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var frames = new PresentationFrameBuilder();
		var spec = AbilityHudCatalog.ForUnit(battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).Type)
			.First(entry => entry.Mode == EPlayerMode.Railgun);

		frames.Interaction.SetMode(EPlayerMode.Railgun, spec);

		var frame = frames.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.True(frame.Instruction.Visible);
		Assert.Equal(BattleHudCopy.PickFiringMount, frame.Instruction.Label);
	}

	[Fact]
	public void FlakShowsPassiveMountInstruction()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var frames = new PresentationFrameBuilder();
		var spec = AbilityHudCatalog.ForUnit(battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).Type)
			.First(entry => entry.Mode == EPlayerMode.Flak);

		frames.Interaction.SetMode(EPlayerMode.Flak, spec);

		var frame = frames.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.True(frame.Instruction.Visible);
		Assert.Equal(BattleHudCopy.PickFiringMount, frame.Instruction.Label);
		Assert.Equal(2, frame.AbilityChoices.Count);
		Assert.Null(frame.HoveredAbilityChoice);
	}

	[Fact]
	public void AbilityHoverPublishesResolvedChoice()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var frames = new PresentationFrameBuilder();
		var spec = AbilityHudCatalog.ForUnit(
				battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).Type)
			.First(entry => entry.Mode == EPlayerMode.Flak);
		frames.Interaction.SetMode(EPlayerMode.Flak, spec);
		frames.Interaction.SetAbilityHover(1, optionCount: 2);

		var frame = frames.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.Equal(1, frame.AbilityHoveredIndex);
		var choice = Assert.IsType<AbilityActivationChoice>(frame.HoveredAbilityChoice);
		Assert.Same(frame.AbilityChoices[1], choice);
		Assert.IsType<FlakAction>(choice.Action);
	}

	[Fact]
	public void BlockedTorpedoMountIsNotPublishedAsChoice()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var (blockedMount, _, _) = TorpedoMount.LaunchPose(
			player.State,
			ESpatialOrientation.Dorsal);
		var battle = BattleTestFixture.BeginSimulation(
			player,
			enemy,
			BattleTestFixture.Grid(),
			new HashSet<Coord> { enemy.State.Position, blockedMount });
		var frames = new PresentationFrameBuilder();
		var spec = AbilityHudCatalog.ForUnit(player.State.Type)
			.First(entry => entry.Mode == EPlayerMode.Torpedo);
		frames.Interaction.SetMode(EPlayerMode.Torpedo, spec);

		var frame = frames.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.Equal(BattleHudCopy.PickFiringMount, frame.Instruction.Label);
		Assert.DoesNotContain(
			frame.AbilityChoices,
			choice => choice.Position == blockedMount);
	}

	[Fact]
	public void ActionFailureShowsUnavailableInstruction()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var frames = new PresentationFrameBuilder();
		var spec = AbilityHudCatalog.ForUnit(battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).Type)
			.First(entry => entry.Mode == EPlayerMode.Railgun);
		frames.Interaction.SetMode(EPlayerMode.Railgun, spec);
		frames.Interaction.ReportActionFailure();

		var frame = frames.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.Equal(BattleHudCopy.ActionUnavailable, frame.Instruction.Label);
	}

	[Fact]
	public void MoveModeHidesInstruction()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var frames = new PresentationFrameBuilder();

		var frame = frames.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.False(frame.Instruction.Visible);
	}

	[Fact]
	public void InspectingHidesInstruction()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var frames = new PresentationFrameBuilder();
		var spec = AbilityHudCatalog.ForUnit(battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).Type)
			.First(entry => entry.Mode == EPlayerMode.Railgun);

		frames.Interaction.SetMode(EPlayerMode.Railgun, spec);
		frames.Interaction.FocusUnit(BattleTestFixture.FirstEnemyId(battle));

		var frame = frames.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.False(frame.Instruction.Visible);
	}
}
