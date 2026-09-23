using GrimSpace.Education;
using GrimSpace.Run;
using GrimSpace.Tutorials;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts.Generation;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Contracts.Generation;

public sealed class ContractGenerationIntegrationTests(StarMapFixture maps)
{
	[Fact]
	public void AdvanceTick_WithGenerationEnabled_TopsUpGeneratedBoardOnCadence()
	{
		using var orchestrator = StarSystemTestHarness.CreatePlayerOrchestrator(
			maps,
			State.PlayerFleetUnitId,
			seed: 42);
		orchestrator.SetContractGenerationEnabled(true);

		orchestrator.AdvanceTicks(ContractBoardConfig.DefaultCadenceTicks);

		var generated = orchestrator.Map.ContractRegistry.Pending
			.Where(contract => !contract.IsStoryObjective)
			.ToList();
		Assert.Equal(3, generated.Count);
	}

	[Fact]
	public void AdvanceTick_WithGenerationDisabled_LeavesOnlyStoryOffers()
	{
		using var orchestrator = StarSystemTestHarness.CreatePlayerOrchestrator(
			maps,
			State.PlayerFleetUnitId,
			seed: 42);
		orchestrator.SetContractGenerationEnabled(false);

		orchestrator.AdvanceTicks(ContractBoardConfig.DefaultCadenceTicks * 2);

		var pending = orchestrator.Map.ContractRegistry.Pending.ToList();
		Assert.Single(pending);
		Assert.True(pending[0].IsStoryObjective);
	}

	[Fact]
	public void CreateNewRun_WithTutorialsDisabled_EnablesContractGeneration()
	{
		using var run = State.CreateNewRun(seed: 42, tutorialsEnabled: false);

		Assert.True(run.StarSystem.ContractGenerationEnabled);
	}

	[Fact]
	public void ConfigureTutorials_Off_EnablesContractGeneration()
	{
		using var run = State.CreateNewRun(seed: 42, tutorialsEnabled: true);
		Assert.False(run.StarSystem.ContractGenerationEnabled);

		run.ConfigureTutorials(false);

		Assert.True(run.StarSystem.ContractGenerationEnabled);
	}

	[Fact]
	public void RunState_GraduationFlowCompleted_EnablesContractGeneration()
	{
		using var run = State.CreateNewRun(seed: 42, tutorialsEnabled: true);
		Assert.False(run.StarSystem.ContractGenerationEnabled);

		var flow = new TutorialFlow(
			TutorialController.GraduationFlowId,
			[
				new TutorialStep(
					null,
					new TutorialDialogContent("Tutorial complete", "Done."),
					ShowIndicator: false,
					FocusTarget: false),
			]);
		Assert.True(run.Tutorials!.TryStartFlow(flow));
		run.Tutorials.AdvanceActive();

		Assert.True(run.StarSystem.ContractGenerationEnabled);
	}

	[Fact]
	public void RunState_RegenerateMap_PreservesTutorialGenerationGate()
	{
		using var run = State.CreateNewRun(seed: 42, tutorialsEnabled: true);
		Assert.False(run.StarSystem.ContractGenerationEnabled);

		run.RegenerateMap(99);

		Assert.False(run.StarSystem.ContractGenerationEnabled);
	}
}
