using GrimSpace.Battle.Actions;
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
	public void RailgunModeShowsEnabledConfirmInstruction()
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
		Assert.True(frame.Instruction.CanConfirm);
		Assert.Equal(BattleHudCopy.ConfirmAction, frame.Instruction.Label);
	}

	[Fact]
	public void FlakWithoutStagingShowsDisabledSelectInstruction()
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
		Assert.False(frame.Instruction.CanConfirm);
		Assert.Equal(BattleHudCopy.SelectFiringDirection, frame.Instruction.Label);
	}

	[Fact]
	public void FlakWithStagingShowsEnabledConfirmInstruction()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var frames = new PresentationFrameBuilder();
		var spec = AbilityHudCatalog.ForUnit(battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).Type)
			.First(entry => entry.Mode == EPlayerMode.Flak);

		frames.Interaction.SetMode(EPlayerMode.Flak, spec);
		frames.Interaction.StageMountedOn(ESpatialOrientation.Port);

		var frame = frames.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.True(frame.Instruction.Visible);
		Assert.True(frame.Instruction.CanConfirm);
		Assert.Equal(BattleHudCopy.ConfirmAction, frame.Instruction.Label);
		Assert.Equal(ESpatialOrientation.Port, frame.StagedMountedOn);
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
