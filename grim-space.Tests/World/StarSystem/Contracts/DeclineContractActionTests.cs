using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Agents;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.Tests.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

public sealed class DeclineContractActionTests
{
	[Fact]
	public void TryEnqueue_SucceedsForOfferedContract()
	{
		var orchestrator = CreateOrchestrator();
		var agent = orchestrator.PlayerAgent!;
		var contractId = orchestrator.Map.ContractRegistry.Offered.First().Id;

		Assert.True(agent.TryEnqueue([new DeclineContractAction(State.PlayerFleetUnitId, contractId)]));
	}

	[Fact]
	public void Commit_PersistsRejection()
	{
		var orchestrator = CreateOrchestrator();
		var agent = orchestrator.PlayerAgent!;
		var contractId = orchestrator.Map.ContractRegistry.Offered.First().Id;

		Assert.True(agent.TryEnqueue([new DeclineContractAction(State.PlayerFleetUnitId, contractId)]));
		orchestrator.AdvanceClock();

		Assert.False(orchestrator.Map.ContractRegistry.IsOffered(contractId));
		Assert.True(orchestrator.Map.ContractRegistry.IsRejected(contractId));
	}

	[Fact]
	public void InteractiveMode_CommitsOnEnqueue()
	{
		var orchestrator = CreateOrchestrator();
		var contractId = orchestrator.Map.ContractRegistry.Offered.First().Id;

		orchestrator.EnterInteractive();
		Assert.True(orchestrator.PlayerAgent!.TryEnqueue(
			[new DeclineContractAction(State.PlayerFleetUnitId, contractId)]));

		Assert.False(orchestrator.Map.ContractRegistry.IsOffered(contractId));
		Assert.True(orchestrator.Map.ContractRegistry.IsRejected(contractId));
	}

	[Fact]
	public void TryEnqueue_FailsForAlreadyRejectedContract()
	{
		var orchestrator = CreateOrchestrator();
		var contractId = orchestrator.Map.ContractRegistry.Offered.First().Id;
		orchestrator.Map.ContractRegistry.Activate(new ContractState(
			contractId,
			EContractStatus.Rejected,
			null,
			null,
			ContractState.EmptyBindings));
		var agent = orchestrator.PlayerAgent!;

		Assert.False(agent.TryEnqueue([new DeclineContractAction(State.PlayerFleetUnitId, contractId)]));
	}

	[Fact]
	public void AdvanceClock_ThenAdvanceTick_DoesNotDoubleCommit()
	{
		var orchestrator = CreateOrchestrator();
		var contractId = orchestrator.Map.ContractRegistry.Offered.First().Id;

		Assert.True(orchestrator.PlayerAgent!.TryEnqueue(
			[new DeclineContractAction(State.PlayerFleetUnitId, contractId)]));
		orchestrator.AdvanceClock();

		Assert.True(orchestrator.Map.ContractRegistry.IsRejected(contractId));

		orchestrator.AdvanceTick();

		Assert.True(orchestrator.Map.ContractRegistry.IsRejected(contractId));
	}

	private static StarSystemOrchestrator CreateOrchestrator() =>
		StarSystemTestHarness.CreatePlayerOrchestrator(State.PlayerFleetUnitId, 42);
}
