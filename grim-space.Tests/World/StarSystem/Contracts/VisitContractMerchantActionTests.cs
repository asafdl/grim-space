using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts.Generation;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class ContractOperatorVisitTests(StarMapFixture maps)
{
	[Fact]
	public void Visit_PausesOnlyVisitedOperatorOutsideTimeline()
	{
		using var orchestrator = StarSystemTestHarness.CreatePlayerOrchestrator(
			maps,
			State.PlayerFleetUnitId,
			seed: 42);
		var currentTick = orchestrator.Tick;
		var (poi, facilityId, operatorName) = AddContractOperator(orchestrator);
		var historyCount = orchestrator.Map.Timeline.ToSnapshot().History.Count;

		Assert.True(orchestrator.TryVisitContractOperator(
			poi.Id,
			facilityId,
			operatorName));

		Assert.True(poi.OperatorTemporaryRoles.IsContractPlacementPaused(
			facilityId,
			operatorName,
			currentTick + ContractBoardConfig.DefaultContractOperatorVisitPauseTicks - 1));
		Assert.False(poi.OperatorTemporaryRoles.IsContractPlacementPaused(
			facilityId,
			operatorName,
			currentTick + ContractBoardConfig.DefaultContractOperatorVisitPauseTicks));
		Assert.Equal(historyCount, orchestrator.Map.Timeline.ToSnapshot().History.Count);
	}

	[Fact]
	public void Visit_RejectsUnknownOperator()
	{
		using var orchestrator = StarSystemTestHarness.CreatePlayerOrchestrator(
			maps,
			State.PlayerFleetUnitId,
			seed: 42);
		var (poi, facilityId, _) = AddContractOperator(orchestrator);

		Assert.False(orchestrator.TryVisitContractOperator(
			poi.Id,
			facilityId,
			"missing-operator"));
	}

	private static (PointOfInterest Poi, string FacilityId, string OperatorName)
		AddContractOperator(StarSystemOrchestrator orchestrator)
	{
		var (poi, facility, facilityOperator) = orchestrator.Map.PointsOfInterest
			.SelectMany(poi => poi.Facilities.SelectMany(facility =>
				facility.Operators.Select(facilityOperator => (poi, facility, facilityOperator))))
			.Where(candidate => !candidate.poi.OperatorTemporaryRoles.TryGetRole(
				candidate.facility.Id,
				candidate.facilityOperator.Name,
				out _))
			.First();

		poi.OperatorTemporaryRoles.Grant(
			facility.Id,
			facilityOperator.Name,
			EFacilityOperatorRole.Contracts,
			"contract-test",
			acceptsSourcesUntilTick: orchestrator.Tick + 30);
		return (poi, facility.Id, facilityOperator.Name);
	}
}
