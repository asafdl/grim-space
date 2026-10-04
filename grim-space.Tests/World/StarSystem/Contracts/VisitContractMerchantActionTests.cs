using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts.Generation;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Poi;
using GrimSpace.Tests.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class VisitContractMerchantActionTests(StarMapFixture maps)
{
	[Fact]
	public void Commit_PausesIssuerGenerationForConfiguredCooldown()
	{
		using var orchestrator = StarSystemTestHarness.CreatePlayerOrchestrator(
			maps,
			State.PlayerFleetUnitId,
			seed: 42);
		var currentTick = orchestrator.Tick;
		var action = CreateAction(orchestrator);

		orchestrator.CommitSetup(action);

		Assert.True(orchestrator.Map.ContractRegistry.IsIssuerGenerationCoolingDown(
			action.PoiId,
			currentTick + ContractBoardConfig.DefaultMerchantRefreshCooldownTicks - 1));
		Assert.False(orchestrator.Map.ContractRegistry.IsIssuerGenerationCoolingDown(
			action.PoiId,
			currentTick + ContractBoardConfig.DefaultMerchantRefreshCooldownTicks));
	}

	[Fact]
	public void TryEnqueue_RejectsUnknownOperator()
	{
		using var orchestrator = StarSystemTestHarness.CreatePlayerOrchestrator(
			maps,
			State.PlayerFleetUnitId,
			seed: 42);
		var action = CreateAction(orchestrator) with { OperatorName = "missing-operator" };
		var simulation = orchestrator.CreateSimulation();

		Assert.False(simulation.TryEnqueue(action));
		Assert.Empty(simulation.Actions);
	}

	private static VisitContractMerchantAction CreateAction(StarSystemOrchestrator orchestrator) =>
		new(
			State.PlayerFleetUnitId,
			ContractActionTestContext.AdministrativePoiId,
			ContractActionTestContext.ManagementFacilityId,
			MapFacilityOperators.ContractOperatorName(orchestrator.Map));
}
