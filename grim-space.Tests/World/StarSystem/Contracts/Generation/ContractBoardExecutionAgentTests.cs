using GrimSpace.Core.Engine;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Agents;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Generation;
using GrimSpace.World.StarSystem.Landmarks;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Contracts.Generation;

[StarSystemTestSuite]
public sealed class ContractBoardExecutionAgentTests(StarMapFixture maps)
{
	[Fact]
	public void Plan_WhenGenerationDisabled_WithDueExpiry_ExpiresOnly()
	{
		var map = maps.Fresh(42);
		var due = ContractFactory.Build(
			map,
			"due-generated",
			EContractKind.Hunt,
			CreateGeneratedHuntArgs(map));
		map.ContractRegistry.TryAdd(due, expiresAtTick: 5);
		var action = PlanAtTick(map, tick: 5, generationEnabled: false);

		Assert.NotNull(action);
		Assert.Equal(5, action.Tick);
		Assert.Empty(action.Additions);
	}

	[Fact]
	public void Plan_OnCadence_FillsGeneratedBoard()
	{
		var map = maps.Fresh(42);
		var config = new ContractBoardConfig
		{
			CadenceTicks = 5,
			Placement = new ContractPlacementConfig { TargetGeneratedCount = 3 },
		};
		var action = PlanAtTick(map, tick: 5, generationEnabled: true, config);

		Assert.NotNull(action);
		Assert.Equal(3, StandardAdditions(action).Count);
		Assert.All(action.Additions, addition => Assert.False(addition.Contract.IsStoryObjective));
		Assert.All(
			action.Additions,
			addition => Assert.Equal(5 + ContractBoardConfig.DefaultTtlTicks, addition.ExpiresAtTick));
	}

	[Fact]
	public void Plan_ExistingLeaseReceivesNewContract()
	{
		var map = maps.Fresh(42);
		var existing = ContractFactory.Build(
			map,
			"existing-generated",
			EContractKind.Hunt,
			CreateGeneratedHuntArgs(map));
		map.ContractRegistry.TryAdd(existing, expiresAtTick: 50);
		var candidate = FirstEligibleOperator(map);
		map.GetPointOfInterest(candidate.PoiId).OperatorTemporaryRoles.Grant(
			candidate.FacilityId,
			candidate.OperatorName,
			EFacilityOperatorRole.Contracts,
			existing.Id,
			acceptsSourcesUntilTick: 10);
		var config = new ContractBoardConfig
		{
			CadenceTicks = 1,
			Placement = new ContractPlacementConfig { TargetGeneratedCount = 2 },
		};

		var action = PlanAtTick(map, tick: 1, generationEnabled: true, config);

		Assert.NotNull(action);
		CreateEngine(map).Commit(action);
		ReconcilePresentations(map, config);
		var (poiId, assignment) = Assert.Single(ContractAssignments(map));
		Assert.Equal(candidate.PoiId, poiId);
		Assert.Equal(candidate.FacilityId, assignment.FacilityId);
		Assert.Equal(candidate.OperatorName, assignment.OperatorName);
		Assert.Equal(10, assignment.AcceptsSourcesUntilTick);
		var added = Assert.Single(StandardAdditions(action));
		Assert.Equal(
			new[] { "existing-generated", added.Contract.Id }.Order(),
			assignment.SourceIds.Order());
	}

	[Fact]
	public void Plan_ExpiredLeaseRetainsExistingContractAndCreatesNewGiver()
	{
		var map = maps.Fresh(42);
		var existing = ContractFactory.Build(
			map,
			"existing-generated",
			EContractKind.Hunt,
			CreateGeneratedHuntArgs(map));
		map.ContractRegistry.TryAdd(existing, expiresAtTick: 50);
		var candidate = FirstEligibleOperator(map);
		map.GetPointOfInterest(candidate.PoiId).OperatorTemporaryRoles.Grant(
			candidate.FacilityId,
			candidate.OperatorName,
			EFacilityOperatorRole.Contracts,
			existing.Id,
			acceptsSourcesUntilTick: 1);
		var config = new ContractBoardConfig
		{
			CadenceTicks = 1,
			Placement = new ContractPlacementConfig { TargetGeneratedCount = 2 },
		};

		var action = PlanAtTick(map, tick: 1, generationEnabled: true, config);

		Assert.NotNull(action);
		CreateEngine(map).Commit(action);
		ReconcilePresentations(map, config);
		var assignments = ContractAssignments(map);
		Assert.Equal(2, assignments.Count);
		var existingAssignment = assignments.Single(item =>
			item.Assignment.SourceIds.Contains(existing.Id));
		Assert.Equal(candidate.OperatorName, existingAssignment.Assignment.OperatorName);
		var addedId = Assert.Single(StandardAdditions(action)).Contract.Id;
		var newAssignment = assignments.Single(item =>
			item.Assignment.SourceIds.Contains(addedId));
		Assert.NotEqual(
			(existingAssignment.PoiId, existingAssignment.Assignment.FacilityId, existingAssignment.Assignment.OperatorName),
			(newAssignment.PoiId, newAssignment.Assignment.FacilityId, newAssignment.Assignment.OperatorName));
		Assert.Equal(
			1 + ContractBoardConfig.DefaultContractGiverLeaseTicks,
			newAssignment.Assignment.AcceptsSourcesUntilTick);
	}

	[Fact]
	public void Reconcile_SuspendedGiverRetainsContractWithoutReceivingAnother()
	{
		var map = maps.Fresh(42);
		var existing = ContractFactory.Build(
			map,
			"existing-generated",
			EContractKind.Hunt,
			CreateGeneratedHuntArgs(map));
		var unassigned = ContractFactory.Build(
			map,
			"unassigned-generated",
			EContractKind.Hunt,
			CreateGeneratedHuntArgs(map));
		map.ContractRegistry.TryAdd(existing, expiresAtTick: 50);
		map.ContractRegistry.TryAdd(unassigned, expiresAtTick: 50);
		var candidate = FirstEligibleOperator(map);
		var roles = map.GetPointOfInterest(candidate.PoiId).OperatorTemporaryRoles;
		roles.Grant(
			candidate.FacilityId,
			candidate.OperatorName,
			EFacilityOperatorRole.Contracts,
			existing.Id,
			acceptsSourcesUntilTick: 10);
		roles.GrantPrimary(
			candidate.FacilityId,
			candidate.OperatorName,
			EFacilityOperatorRole.DeliveryTurnIn,
			"delivery-contract");

		ReconcilePresentations(map);

		Assert.True(roles.TryGetRole(
			candidate.FacilityId,
			candidate.OperatorName,
			out var activeRole));
		Assert.Equal(EFacilityOperatorRole.DeliveryTurnIn, activeRole);
		var visibleAssignments = ContractAssignments(map);
		Assert.DoesNotContain(
			visibleAssignments,
			item => item.Assignment.SourceIds.Contains(existing.Id));
		Assert.Contains(
			visibleAssignments,
			item => item.Assignment.SourceIds.Contains(unassigned.Id));

		roles.RevokeBySource("delivery-contract");

		Assert.True(roles.TryGetRole(
			candidate.FacilityId,
			candidate.OperatorName,
			out activeRole));
		Assert.Equal(EFacilityOperatorRole.Contracts, activeRole);
		var restored = Assert.Single(ContractAssignments(map), item =>
			item.Assignment.SourceIds.Contains(existing.Id));
		Assert.Equal(candidate.PoiId, restored.PoiId);
		Assert.Equal(candidate.FacilityId, restored.Assignment.FacilityId);
		Assert.Equal(candidate.OperatorName, restored.Assignment.OperatorName);
	}

	[Fact]
	public void Reconcile_WhenGiverHasTwoContracts_StartsAnotherGiver()
	{
		var map = maps.Fresh(42);
		for (var index = 0; index < 3; index++)
		{
			var contract = ContractFactory.Build(
				map,
				$"generated-{index}",
				EContractKind.Hunt,
				CreateGeneratedHuntArgs(map));
			map.ContractRegistry.TryAdd(contract, expiresAtTick: 50);
		}

		ReconcilePresentations(map);

		var assignments = ContractAssignments(map);
		Assert.Equal(2, assignments.Count);
		Assert.Equal(
			[1, 2],
			assignments.Select(item => item.Assignment.SourceIds.Count).Order());
	}

	[Fact]
	public void Plan_MissingAssignments_RedistributesPendingContractsOffCadence()
	{
		var map = maps.Fresh(42);
		var existing = ContractFactory.Build(
			map,
			"orphaned-generated",
			EContractKind.Hunt,
			CreateGeneratedHuntArgs(map));
		map.ContractRegistry.TryAdd(existing, expiresAtTick: 50);

		var action = PlanAtTick(map, tick: 4, generationEnabled: false);

		Assert.NotNull(action);
		Assert.Empty(action.Additions);
		CreateEngine(map).Commit(action);
		ReconcilePresentations(map);
		Assert.Equal(
			["orphaned-generated"],
			Assert.Single(ContractAssignments(map)).Assignment.SourceIds);
	}

	[Fact]
	public void Plan_NoIdleNpc_RenewsExistingGiver()
	{
		var map = maps.Fresh(42);
		var existing = ContractFactory.Build(
			map,
			"existing-generated",
			EContractKind.Hunt,
			CreateGeneratedHuntArgs(map));
		map.ContractRegistry.TryAdd(existing, expiresAtTick: 50);
		var candidate = FirstEligibleOperator(map);
		map.GetPointOfInterest(candidate.PoiId).OperatorTemporaryRoles.Grant(
			candidate.FacilityId,
			candidate.OperatorName,
			EFacilityOperatorRole.Contracts,
			existing.Id,
			acceptsSourcesUntilTick: 1);
		var blockIndex = 0;
		foreach (var other in EligibleOperators(map).Where(other => other != candidate))
		{
			map.GetPointOfInterest(other.PoiId).OperatorTemporaryRoles.Grant(
				other.FacilityId,
				other.OperatorName,
				EFacilityOperatorRole.DeliveryTurnIn,
				$"block-{blockIndex++}");
		}
		var config = new ContractBoardConfig
		{
			CadenceTicks = 1,
			ContractGiverLeaseTicks = 7,
			Placement = new ContractPlacementConfig { TargetGeneratedCount = 2 },
		};

		var action = PlanAtTick(map, tick: 1, generationEnabled: true, config);

		Assert.NotNull(action);
		CreateEngine(map).Commit(action);
		ReconcilePresentations(map, config);
		var assignment = Assert.Single(ContractAssignments(map)).Assignment;
		Assert.Equal(candidate.OperatorName, assignment.OperatorName);
		Assert.Equal(8, assignment.AcceptsSourcesUntilTick);
		Assert.Equal(2, assignment.SourceIds.Count);
	}

	[Fact]
	public void Plan_OffCadence_PublishesWithNoAdditions()
	{
		var map = maps.Fresh(42);
		var action = PlanAtTick(map, tick: 4, generationEnabled: true);

		Assert.NotNull(action);
		Assert.Empty(action.Additions);
	}

	[Fact]
	public void Plan_WhenOfferExpired_PublishesOffCadenceWithNoAdditions()
	{
		var map = maps.Fresh(42);
		var due = ContractFactory.Build(
			map,
			"due-generated",
			EContractKind.Hunt,
			CreateGeneratedHuntArgs(map));
		map.ContractRegistry.TryAdd(due, expiresAtTick: 7);

		var action = PlanAtTick(map, tick: 7, generationEnabled: false);
		Assert.NotNull(action);
		Assert.Empty(action.Additions);
	}

	[Fact]
	public void Plan_RespectsMaxPendingCap()
	{
		var map = maps.Fresh(42);
		map.ContractRegistry.MaxPending = 2;
		var config = new ContractBoardConfig
		{
			CadenceTicks = 1,
			Placement = new ContractPlacementConfig { TargetGeneratedCount = 3 },
		};
		var action = PlanAtTick(map, tick: 1, generationEnabled: true, config);

		Assert.NotNull(action);
		Assert.Equal(2, StandardAdditions(action).Count);
	}

	[Fact]
	public void Plan_ActiveGeneratedContracts_OccupyBoardSlots()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, State.PlayerFleetUnitId);
		var active = ContractFactory.Build(
			map,
			"active-generated",
			EContractKind.Hunt,
			CreateGeneratedHuntArgs(map));
		map.ContractRegistry.TryAdd(active, expiresAtTick: 50);
		map.ContractRegistry.Activate(new ContractState(
			active.Id,
			EContractStatus.Active,
			AcceptedAtTick: 1,
			State.PlayerFleetUnitId));
		var config = new ContractBoardConfig
		{
			CadenceTicks = 1,
			Placement = new ContractPlacementConfig { TargetGeneratedCount = 3 },
		};

		var action = PlanAtTick(map, tick: 1, generationEnabled: true, config);

		Assert.NotNull(action);
		Assert.Equal(2, StandardAdditions(action).Count);
	}

	[Fact]
	public void Plan_SameInputs_ProduceDeterministicContractIds()
	{
		var map = maps.Fresh(99);
		var first = PlanAtTick(map, tick: 10, generationEnabled: true);
		var second = PlanAtTick(map, tick: 10, generationEnabled: true);

		Assert.NotNull(first);
		Assert.NotNull(second);
		Assert.Equal(
			first.Additions.Select(addition => addition.Contract.Id),
			second.Additions.Select(addition => addition.Contract.Id));
	}

	[Fact]
	public void Plan_LeadChancePasses_AddsOneLeadWithoutChangingStandardTarget()
	{
		var map = maps.Fresh(42);
		var config = new ContractBoardConfig
		{
			CadenceTicks = 1,
			TtlTicks = 40,
			Placement = new ContractPlacementConfig { TargetGeneratedCount = 3 },
			Leads = new LeadGenerationConfig
			{
				GenerationChance = 1,
				SaleDurationTicks = 120,
				SpaceMeetingChance = 1,
			},
		};

		var action = PlanAtTick(map, tick: 5, generationEnabled: true, config);

		Assert.NotNull(action);
		Assert.Equal(3, StandardAdditions(action).Count);
		var lead = Assert.Single(LeadAdditions(action));
		Assert.Equal(45, lead.ExpiresAtTick);
		var objective = Assert.IsType<BlackMarketSaleObjective>(lead.Contract.Objective);
		Assert.Equal(120, objective.DurationTicks);
		Assert.IsType<BlackMarketSpaceMeetingDestination>(objective.Destination);
	}

	[Fact]
	public void Plan_ExistingLeadOccupancy_BlocksAnotherLead()
	{
		var map = maps.Fresh(42);
		var existing = ContractFactory.Build(
			map,
			"existing-lead",
			EContractKind.Lead,
			new LeadCreateArgs(
				ELeadKind.BlackMarketSale,
				new ContractNarrative("Existing lead", "Already offered."),
				300));
		Assert.True(map.ContractRegistry.TryAdd(existing, expiresAtTick: 50));
		var config = new ContractBoardConfig
		{
			CadenceTicks = 1,
			Placement = new ContractPlacementConfig { TargetGeneratedCount = 0 },
			Leads = new LeadGenerationConfig { GenerationChance = 1 },
		};

		var action = PlanAtTick(map, tick: 5, generationEnabled: true, config);

		Assert.NotNull(action);
		Assert.Empty(action.Additions);
	}

	[Fact]
	public void Plan_LeadGeneration_DoesNotPerturbStandardGeneration()
	{
		var withoutLead = maps.Fresh(73);
		var withLead = maps.Fresh(73);
		var disabledConfig = new ContractBoardConfig
		{
			CadenceTicks = 1,
			Leads = new LeadGenerationConfig { GenerationChance = 0 },
		};
		var enabledConfig = new ContractBoardConfig
		{
			CadenceTicks = 1,
			Leads = new LeadGenerationConfig { GenerationChance = 1 },
		};

		var disabled = PlanAtTick(withoutLead, tick: 10, generationEnabled: true, disabledConfig);
		var enabled = PlanAtTick(withLead, tick: 10, generationEnabled: true, enabledConfig);

		Assert.NotNull(disabled);
		Assert.NotNull(enabled);
		Assert.Equal(
			StandardAdditions(disabled).Select(StandardSignature),
			StandardAdditions(enabled).Select(StandardSignature));
		Assert.Empty(LeadAdditions(disabled));
		Assert.Single(LeadAdditions(enabled));
	}

	[Fact]
	public void Plan_SameSeedAndTick_ProducesSameLeadChanceResult()
	{
		var firstMap = maps.Fresh(91);
		var secondMap = maps.Fresh(91);
		var config = new ContractBoardConfig
		{
			CadenceTicks = 1,
			Placement = new ContractPlacementConfig { TargetGeneratedCount = 0 },
			Leads = new LeadGenerationConfig { GenerationChance = 0.5 },
		};

		var first = PlanAtTick(firstMap, tick: 15, generationEnabled: true, config);
		var second = PlanAtTick(secondMap, tick: 15, generationEnabled: true, config);

		Assert.NotNull(first);
		Assert.NotNull(second);
		Assert.Equal(
			LeadAdditions(first).Select(addition => addition.Contract.Id),
			LeadAdditions(second).Select(addition => addition.Contract.Id));
	}

	[Fact]
	public void Reconcile_PendingLeadGetsDedicatedContactAwayFromSaleDestination()
	{
		var map = maps.Fresh(42);
		var lead = ContractFactory.Build(
			map,
			"lead-contact",
			EContractKind.Lead,
			new LeadCreateArgs(
				ELeadKind.BlackMarketSale,
				new ContractNarrative("Quiet Opportunity", "A seller is available."),
				300,
				SpaceMeetingChance: 0));
		Assert.True(map.ContractRegistry.TryAdd(lead, expiresAtTick: 100));

		ReconcilePresentations(map);

		var (poiId, assignment) = Assert.Single(LeadAssignments(map));
		Assert.Equal(["lead-contact"], assignment.SourceIds);
		Assert.Equal(EOperatorAssignmentPriority.Presentation, assignment.Priority);
		var objective = Assert.IsType<BlackMarketSaleObjective>(lead.Objective);
		var destination = Assert.IsType<BlackMarketFacilityDestination>(objective.Destination);
		Assert.NotEqual(destination.PoiId, poiId);
		var facility = map.GetPointOfInterest(poiId).GetFacility(assignment.FacilityId);
		var facilityOperator = facility.Operators.Single(candidate =>
			candidate.Name == assignment.OperatorName);
		Assert.Equal(EFacilityOperatorRole.Dialog, facilityOperator.Role);
		Assert.DoesNotContain(
			ContractAssignments(map),
			candidate => candidate.Assignment.SourceIds.Contains(lead.Id));
	}

	[Fact]
	public void Reconcile_MultiplePendingLeadsNeverShareContact()
	{
		var map = maps.Fresh(42);
		foreach (var contractId in new[] { "lead-a", "lead-b" })
		{
			var lead = ContractFactory.Build(
				map,
				contractId,
				EContractKind.Lead,
				new LeadCreateArgs(
					ELeadKind.BlackMarketSale,
					new ContractNarrative("Quiet Opportunity", "A seller is available."),
					300,
					SpaceMeetingChance: 0));
			Assert.True(map.ContractRegistry.TryAdd(lead, expiresAtTick: 100));
		}

		ReconcilePresentations(map);

		var assignments = LeadAssignments(map);
		Assert.Equal(2, assignments.Count);
		Assert.All(assignments, item => Assert.Single(item.Assignment.SourceIds));
		Assert.Equal(
			2,
			assignments
				.Select(item => (
					item.PoiId,
					item.Assignment.FacilityId,
					item.Assignment.OperatorName))
				.Distinct()
				.Count());
	}

	[Fact]
	public void Reconcile_LeadContactPlacementIsStable()
	{
		var map = maps.Fresh(42);
		var lead = ContractFactory.Build(
			map,
			"lead-stable-contact",
			EContractKind.Lead,
			new LeadCreateArgs(
				ELeadKind.BlackMarketSale,
				new ContractNarrative("Quiet Opportunity", "A seller is available."),
				300,
				SpaceMeetingChance: 0));
		Assert.True(map.ContractRegistry.TryAdd(lead, expiresAtTick: 100));
		ReconcilePresentations(map);
		var first = Assert.Single(LeadAssignments(map));

		ReconcilePresentations(map);

		var second = Assert.Single(LeadAssignments(map));
		Assert.Equal(first.PoiId, second.PoiId);
		Assert.Equal(first.Assignment.FacilityId, second.Assignment.FacilityId);
		Assert.Equal(first.Assignment.OperatorName, second.Assignment.OperatorName);
		Assert.Equal(first.Assignment.SourceIds, second.Assignment.SourceIds);
	}

	[Fact]
	public void Reconcile_AcceptedFacilityLead_RedistributesDisplacedContract()
	{
		var map = maps.Fresh(42);
		var standard = ContractFactory.Build(
			map,
			"displaced-contract",
			EContractKind.Hunt,
			CreateGeneratedHuntArgs(map));
		Assert.True(map.ContractRegistry.TryAdd(standard, expiresAtTick: 100));
		var lead = ContractFactory.Build(
			map,
			"merchant-takeover",
			EContractKind.Lead,
			new LeadCreateArgs(
				ELeadKind.BlackMarketSale,
				new ContractNarrative("Quiet Opportunity", "A seller is available."),
				300,
				SpaceMeetingChance: 0));
		Assert.True(map.ContractRegistry.TryAdd(lead, expiresAtTick: 100));
		var holderId = map.FleetRegistry.All.First().State.Id;
		Assert.True(LeadContractActivation.TryBuildActiveStateOnAccept(
			map,
			lead,
			holderId,
			map.Timeline.Clock.Current,
			out var preview));
		var destination = Assert.IsType<BlackMarketFacilityDestination>(
			Assert.IsType<BlackMarketSaleObjective>(lead.Objective).Destination);
		var displacedOperator = preview.DestinationOperatorName!;
		map.GetPointOfInterest(destination.PoiId).OperatorTemporaryRoles.Grant(
			destination.FacilityId,
			displacedOperator,
			EFacilityOperatorRole.Contracts,
			standard.Id);

		CreateEngine(map).Commit(ContractActionTestContext.Accept(map, holderId, lead.Id));
		ReconcilePresentations(map);

		var reassigned = Assert.Single(
			ContractAssignments(map),
			item => item.Assignment.SourceIds.Contains(standard.Id));
		Assert.NotEqual(
			(destination.PoiId, destination.FacilityId, displacedOperator),
			(reassigned.PoiId, reassigned.Assignment.FacilityId, reassigned.Assignment.OperatorName));
		Assert.True(map.GetPointOfInterest(destination.PoiId).OperatorTemporaryRoles.TryGetAssignment(
			destination.FacilityId,
			displacedOperator,
			out var merchant));
		Assert.Equal(EFacilityOperatorRole.Merchant, merchant.Role);
	}

	[Fact]
	public void Reconcile_DeliveryContract_DoesNotAssignTurnInPoiAsGiver()
	{
		var map = maps.Fresh(42);
		var config = new ContractBoardConfig
		{
			CadenceTicks = 1,
			Placement = new ContractPlacementConfig
			{
				TargetGeneratedCount = 6,
				HuntKindWeight = 0f,
				DeliveryKindWeight = 1f,
				WreckageKindWeight = 0f,
			},
		};
		var action = PlanAtTick(map, tick: 1, generationEnabled: true, config);
		Assert.NotNull(action);
		CreateEngine(map).Commit(action);
		ReconcilePresentations(map, config);

		foreach (var contract in map.ContractRegistry.Pending.Where(contract =>
			contract.Objective is DeliveryObjective))
		{
			var delivery = (DeliveryObjective)contract.Objective;
			var assignment = Assert.Single(
				ContractAssignments(map),
				item => item.Assignment.SourceIds.Contains(contract.Id));
			Assert.NotEqual(delivery.TurnInPoiId, assignment.PoiId);
		}
	}

	[Fact]
	public void Plan_WreckageOnlyWeight_CanProduceWreckageContracts()
	{
		var map = maps.Fresh(55);
		var config = new ContractBoardConfig
		{
			CadenceTicks = 1,
			Placement = new ContractPlacementConfig
			{
				TargetGeneratedCount = 6,
				HuntKindWeight = 0f,
				DeliveryKindWeight = 0f,
				WreckageKindWeight = 1f,
			},
		};
		var action = PlanAtTick(map, tick: 1, generationEnabled: true, config);
		Assert.NotNull(action);
		Assert.Contains(
			action.Additions,
			addition => addition.Contract.Objective is WreckageObjective);
	}

	[Fact]
	public void TryBuildHunt_WhenNoSearchAreaExists_ReturnsFalse()
	{
		var map = maps.Fresh(42);
		var args = new HuntCreateArgs(
			new AreaPickerArgs([]),
			EDangerLevel.VeryLow,
			ContractNarrative.ForHunt("Unavailable Hunt"));

		Assert.False(ContractFactory.TryBuildHunt(
			map,
			"unavailable-hunt",
			args,
			out _));
	}

	[Fact]
	public void Commit_OnCadence_RegistersGeneratedContracts()
	{
		var map = maps.Fresh(42);
		var engine = CreateEngine(map);
		var config = new ContractBoardConfig { CadenceTicks = 1 };
		var action = PlanAtTick(map, engine.Tick, generationEnabled: true, config);
		Assert.NotNull(action);

		engine.Commit(action);
		ReconcilePresentations(engine.World, config);

		var generated = engine.World.ContractRegistry.Pending
			.Where(contract =>
				contract.Category == EContractCategory.Standard
				&& !contract.IsStoryObjective)
			.ToList();
		var assignments = ContractAssignments(engine.World);
		Assert.Equal(6, generated.Count);
		Assert.Equal(3, assignments.Count);
		Assert.All(assignments, item => Assert.Equal(2, item.Assignment.SourceIds.Count));
		Assert.Equal(
			generated.Select(contract => contract.Id).Order(),
			assignments
				.Select(item => item.Assignment)
				.SelectMany(assignment => assignment.SourceIds)
				.Order());
	}

	[Fact]
	public void Commit_ExpiresBeforeCadenceTopUp()
	{
		var map = maps.Fresh(42);
		map.ContractRegistry.MaxPending = 2;
		var engine = CreateEngine(map);
		var config = new ContractBoardConfig { CadenceTicks = 1, TtlTicks = 10 };
		var tick = engine.Tick;
		var due = ContractFactory.Build(
			map,
			"due-generated",
			EContractKind.Hunt,
			CreateGeneratedHuntArgs(map));
		map.ContractRegistry.TryAdd(due, expiresAtTick: tick);
		var action = PlanAtTick(map, tick, generationEnabled: true, config);
		Assert.NotNull(action);

		engine.Commit(action);

		Assert.False(engine.World.ContractRegistry.IsPending("due-generated"));
	}

	private static MaintainContractBoardAction? PlanAtTick(
		StarMap map,
		int tick,
		bool generationEnabled,
		ContractBoardConfig? config = null)
	{
		var sink = new ActionBatchSink();
		map.Timeline.Clock.Set(tick);
		var agent = CreateAgent(map, () => generationEnabled, sink, config);
		agent.PlanAndPublish();
		if (!sink.TryTakeBatch(StarSystemActorIds.Contracts, out var batch) || batch.Actions.Count == 0)
			return null;

		return (MaintainContractBoardAction)batch.Actions[0];
	}

	private static ContractBoardExecutionAgent CreateAgent(
		StarMap map,
		Func<bool> generationEnabled,
		ActionBatchSink sink,
		ContractBoardConfig? config = null)
	{
		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(StarSystemActorIds.Contracts);
		var agent = new ContractBoardExecutionAgent(
			() => map,
			runtimes.For,
			generationEnabled,
			config);
		agent.Init(StarSystemActorIds.Contracts, sink.WriterFor(StarSystemActorIds.Contracts));
		agent.SetCanWork(true);
		return agent;
	}

	private static void ReconcilePresentations(
		StarMap map,
		ContractBoardConfig? config = null)
	{
		var sink = new ActionBatchSink();
		var agent = CreateAgent(map, () => false, sink, config);
		agent.ReconcilePresentations();
	}

	private static HuntCreateArgs CreateGeneratedHuntArgs(StarMap map)
	{
		return new HuntCreateArgs(
			new AreaPickerArgs(MapLandmarkQueries.AllIds(map), DeterministicPickMix: 1),
			EDangerLevel.VeryLow,
			ContractNarrative.ForHunt("Generated Hunt"));
	}

	private static IReadOnlyList<ContractAddition> StandardAdditions(
		MaintainContractBoardAction action) =>
		action.Additions
			.Where(addition => addition.Contract.Category == EContractCategory.Standard)
			.ToArray();

	private static IReadOnlyList<ContractAddition> LeadAdditions(
		MaintainContractBoardAction action) =>
		action.Additions
			.Where(addition => addition.Contract.Category == EContractCategory.Lead)
			.ToArray();

	private static string StandardSignature(ContractAddition addition) =>
		$"{addition.Contract.Id}:{addition.Contract.Objective.GetType().Name}:{addition.ExpiresAtTick}";

	private static (string PoiId, string FacilityId, string OperatorName) FirstEligibleOperator(
		StarMap map) =>
		EligibleOperators(map).First();

	private static IReadOnlyList<(string PoiId, string FacilityId, string OperatorName)> EligibleOperators(
		StarMap map) =>
		map.PointsOfInterest
			.SelectMany(poi => poi.Facilities.SelectMany(facility =>
				facility.Operators
					.Where(facilityOperator =>
						facilityOperator.Role is not (
							EFacilityOperatorRole.Merchant
								or EFacilityOperatorRole.HealingStation))
					.Select(facilityOperator => (
						PoiId: poi.Id,
						FacilityId: facility.Id,
						OperatorName: facilityOperator.Name))))
			.OrderBy(candidate => candidate.PoiId, StringComparer.Ordinal)
			.ThenBy(candidate => candidate.FacilityId, StringComparer.Ordinal)
			.ThenBy(candidate => candidate.OperatorName, StringComparer.Ordinal)
			.ToArray();

	private static IReadOnlyList<(
		string PoiId,
		FacilityOperatorTemporaryRoles.Assignment Assignment)> ContractAssignments(StarMap map) =>
		map.PointsOfInterest
			.SelectMany(poi => poi.OperatorTemporaryRoles
				.Assignments(EFacilityOperatorRole.Contracts)
				.Select(assignment => (poi.Id, assignment)))
			.ToArray();

	private static IReadOnlyList<(
		string PoiId,
		FacilityOperatorTemporaryRoles.Assignment Assignment)> LeadAssignments(StarMap map) =>
		map.PointsOfInterest
			.SelectMany(poi => poi.OperatorTemporaryRoles
				.Assignments(EFacilityOperatorRole.LeadContact)
				.Select(assignment => (poi.Id, assignment)))
			.ToArray();

	private static Engine<StarMap, ActorRuntime> CreateEngine(StarMap map)
	{
		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(StarSystemActorIds.Contracts);
		foreach (var id in map.FleetRegistry.Ids)
			runtimes.For(id);
		return new Engine<StarMap, ActorRuntime>(map, runtimes);
	}
}
