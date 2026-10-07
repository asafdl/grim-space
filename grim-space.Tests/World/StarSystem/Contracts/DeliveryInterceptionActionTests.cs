using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Battle.Objectives;
using GrimSpace.Math.Grid;
using GrimSpace.Run.Persistence;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.FleetSpawner;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Poi;
using GrimSpace.Tests.World.StarSystem.Traffic;
using StrategicEngagement = GrimSpace.World.StarSystem.Units.Engagement;
using BattleUnitType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class DeliveryInterceptionActionTests(StarMapFixture maps)
{
	[Fact]
	public void TrySelectInterceptor_PrefersNearestPathablePirate()
	{
		var map = maps.Fresh(5);
		const string playerId = "player-select";
		StarSystemTestHarness.AddPlayerFleet(map, playerId);
		var player = map.FleetRegistry.FleetOf(playerId);
		player.State.Travel = new FleetTravel.AtRest(new Coord(0, 0, 0));
		AddAmbientPirate(map, "pirate-far", new Coord(30, 0, 30));
		AddAmbientPirate(map, "pirate-near", new Coord(4, 0, 4));

		Assert.True(AttemptDeliveryInterceptionDef.TrySelectInterceptor(
			map,
			playerId,
			out var selected));
		Assert.Equal("pirate-near", selected);
	}

	[Fact]
	public void TrySelectInterceptor_TieBreaksByFleetId()
	{
		var map = maps.Fresh(5);
		const string playerId = "player-tie";
		StarSystemTestHarness.AddPlayerFleet(map, playerId);
		var player = map.FleetRegistry.FleetOf(playerId);
		player.State.Travel = new FleetTravel.AtRest(new Coord(0, 0, 0));
		AddAmbientPirate(map, "pirate-b", new Coord(5, 0, 5));
		AddAmbientPirate(map, "pirate-a", new Coord(5, 0, 5));

		Assert.True(AttemptDeliveryInterceptionDef.TrySelectInterceptor(
			map,
			playerId,
			out var selected));
		Assert.Equal("pirate-a", selected);
	}

	[Fact]
	public void Attempt_ReschedulesWhenPlayerNotInTransit()
	{
		var map = maps.Fresh(5);
		var (contractId, playerId) = SeedPendingDelivery(map);
		AddAmbientPirate(map, "pirate-retry", new Coord(3, 0, 3));

		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(StarSystemActorIds.Contracts);
		var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);
		engine.Commit(new AttemptDeliveryInterceptionAction(
			StarSystemActorIds.Contracts,
			contractId,
			playerId));

		Assert.True(map.Timeline.ContainsPending(action =>
			action is AttemptDeliveryInterceptionAction attempt
			&& attempt.ContractId == contractId
			&& attempt.PlayerFleetId == playerId));
		Assert.DoesNotContain(
			map.Timeline.TakePending(map.Timeline.Clock.Current + 1),
			action => action is AttemptDeliveryInterceptionAction);
		Assert.Contains(
			map.Timeline.TakePending(
				map.Timeline.Clock.Current
				+ DeliveryGenerationConfig.DefaultInterceptionRetryDelayTicks),
			action => action is AttemptDeliveryInterceptionAction);
	}

	[Fact]
	public void Attempt_ReschedulesWhenPlayerIsEngaged()
	{
		var map = maps.Fresh(5);
		var (contractId, playerId) = SeedPendingDelivery(map);
		var player = map.FleetRegistry.FleetOf(playerId);
		SetTraveling(map, playerId);
		player.State.CurrentEngagement = new StrategicEngagement(
			"engagement-retry",
			EEngagementPhase.Engaged,
			playerId,
			new HashSet<string>([playerId], StringComparer.Ordinal),
			null,
			null);
		AddAmbientPirate(map, "pirate-retry", new Coord(3, 0, 3));

		var engine = CreateEngine(map, playerId);
		engine.Commit(new AttemptDeliveryInterceptionAction(
			StarSystemActorIds.Contracts,
			contractId,
			playerId));

		Assert.True(map.Timeline.ContainsPending(action =>
			action is AttemptDeliveryInterceptionAction attempt
			&& attempt.ContractId == contractId));
	}

	[Fact]
	public void Attempt_DoesNotRetryAfterContractEnds()
	{
		var map = maps.Fresh(5);
		var (contractId, playerId) = SeedPendingDelivery(map);
		map.ContractRegistry.Fail(contractId);
		var engine = CreateEngine(map, playerId);

		engine.Commit(new AttemptDeliveryInterceptionAction(
			StarSystemActorIds.Contracts,
			contractId,
			playerId));

		Assert.False(map.Timeline.ContainsPending(action =>
			action is AttemptDeliveryInterceptionAction attempt
			&& attempt.ContractId == contractId));
	}

	[Fact]
	public void Attempt_ReschedulesWhenNoEligibleInterceptorExists()
	{
		var map = maps.Fresh(5);
		var (contractId, playerId) = SeedPendingDelivery(map);
		SetTraveling(map, playerId);
		var engine = CreateEngine(map, playerId);

		engine.Commit(new AttemptDeliveryInterceptionAction(
			StarSystemActorIds.Contracts,
			contractId,
			playerId));

		Assert.True(map.Timeline.ContainsPending(action =>
			action is AttemptDeliveryInterceptionAction attempt
			&& attempt.ContractId == contractId));
	}

	[Fact]
	public void AssignInterceptor_UpdatesContractAndDirective()
	{
		var map = maps.Fresh(5);
		var (contractId, playerId) = SeedPendingDelivery(map);
		var player = map.FleetRegistry.FleetOf(playerId);
		SetTraveling(map, playerId);
		AddAmbientPirate(map, "pirate-assign", new Coord(3, 0, 3));

		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(StarSystemActorIds.Contracts);
		var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);
		engine.Commit(new AttemptDeliveryInterceptionAction(
			StarSystemActorIds.Contracts,
			contractId,
			playerId));

		Assert.True(map.ContractRegistry.TryGetState(contractId, out var state));
		var delivery = Assert.IsType<DeliveryContractState>(state);
		Assert.Equal(EDeliveryInterceptionState.Assigned, delivery.Progress.InterceptionState);
		Assert.Equal("pirate-assign", delivery.Progress.InterceptorFleetId);
		var interceptor = map.FleetRegistry.FleetOf("pirate-assign");
		Assert.NotNull(interceptor.State.PursuitDirective);
		Assert.Equal(contractId, interceptor.State.PursuitDirective.ContractId);
		Assert.Equal(playerId, interceptor.State.PursuitDirective.TargetFleetId);
		Assert.Null(interceptor.State.SourceContractId);
	}

	[Fact]
	public void Orchestrator_AssignedInterceptorBeginsPursuit()
	{
		var map = maps.Fresh(5);
		const string playerId = "player-orchestrated";
		StarSystemTestHarness.AddPlayerFleet(map, playerId);
		var (contractId, _) = SeedPendingDelivery(map, holderId: playerId);
		var player = map.FleetRegistry.FleetOf(playerId);
		var origin = map.DockAt(player.State)!.Position;
		var pathfinder = new GridPathfinder(map.PathfindingTerrain);
		var exit = map.DocksByPoiId[map.Blueprint.SupplyPlan.ExitPoiId].Position;
		var routeToExit = Assert.IsType<PathfindingResult.Found>(
			pathfinder.FindPath(origin, exit)).Path;
		var destination = routeToExit.SampleAtElapsed(20, player.State.SpeedPerTick).Position;
		var playerPath = Assert.IsType<PathfindingResult.Found>(
			pathfinder.FindPath(origin, destination)).Path;
		var piratePosition = playerPath.SampleAtElapsed(
			10,
			UnitDefaults.SpeedPerTick(EType.PirateFleet)).Position;
		AddAmbientPirate(
			map,
			"pirate-orchestrated",
			piratePosition);
		using var orchestrator = StarSystemOrchestrator.FromMap(
			map,
			pathfinder,
			playerId);
		orchestrator.CommitSetup(new MoveAction(
			playerId,
			playerId,
			destination,
			playerPath));
		orchestrator.CommitSetup(new AttemptDeliveryInterceptionAction(
			StarSystemActorIds.Contracts,
			contractId,
			playerId));
		var delivery = Assert.IsType<DeliveryContractState>(
			map.ContractRegistry.TryGetState(contractId, out var state) ? state : null);
		var interceptorId = Assert.IsType<string>(delivery.Progress.InterceptorFleetId);

		orchestrator.AdvanceTick();

		var interceptor = map.FleetRegistry.FleetOf(interceptorId);
		Assert.IsType<FleetTravel.Journey>(interceptor.State.Travel);
		Assert.True(interceptor.State.TravelTarget.MatchesFleet(
			playerId,
			EContactIntent.Engagement));
		var firstJourneyId = interceptor.State.Journey().Id;
		var firstDestination = interceptor.State.Journey().Destination;

		orchestrator.AdvanceTick();

		Assert.Equal(firstJourneyId, interceptor.State.Journey().Id);

		for (var tick = 0;
			tick < EngagementQueries.MaxContactCheckBackoffTicks
			&& interceptor.State.Travel is FleetTravel.Journey { Id: var journeyId }
			&& journeyId == firstJourneyId;
			tick++)
			orchestrator.AdvanceTick();

		Assert.True(interceptor.State.Journey().Id > firstJourneyId);
		Assert.NotEqual(firstDestination, interceptor.State.Journey().Destination);

		for (var tick = 0;
			tick < 50
			&& player.State.CurrentEngagement?.Phase != EEngagementPhase.AwaitingDecision;
			tick++)
			orchestrator.AdvanceTick();

		Assert.True(
			player.State.CurrentEngagement?.Phase == EEngagementPhase.AwaitingDecision,
			$"playerPhase={player.State.CurrentEngagement?.Phase} " +
			$"interceptorPhase={interceptor.State.CurrentEngagement?.Phase} " +
			$"playerPosition={orchestrator.CommittedPositionOf(playerId)} " +
			$"interceptorPosition={orchestrator.CommittedPositionOf(interceptorId)} " +
			$"interceptorTarget={interceptor.State.TravelTarget}");
	}

	[Fact]
	public void Attempt_ConcurrentDeliveriesDoNotShareInterceptor()
	{
		var map = maps.Fresh(5);
		var (firstContractId, playerId) = SeedPendingDelivery(map);
		var secondContractId = SeedPendingDelivery(map, "delivery-interception-2").ContractId;
		var player = map.FleetRegistry.FleetOf(playerId);
		SetTraveling(map, playerId);
		AddAmbientPirate(map, "pirate-first", new Coord(3, 0, 3));
		AddAmbientPirate(map, "pirate-second", new Coord(6, 0, 6));
		var engine = CreateEngine(map, playerId);

		engine.Commit(
			new AttemptDeliveryInterceptionAction(
				StarSystemActorIds.Contracts,
				firstContractId,
				playerId),
			new AttemptDeliveryInterceptionAction(
				StarSystemActorIds.Contracts,
				secondContractId,
				playerId));

		var first = Assert.IsType<DeliveryContractState>(
			map.ContractRegistry.TryGetState(firstContractId, out var firstState)
				? firstState
				: null);
		var second = Assert.IsType<DeliveryContractState>(
			map.ContractRegistry.TryGetState(secondContractId, out var secondState)
				? secondState
				: null);
		Assert.NotEqual(first.Progress.InterceptorFleetId, second.Progress.InterceptorFleetId);
	}

	[Fact]
	public void FleeingAssignedInterceptor_FailsDelivery()
	{
		var map = maps.Fresh(5);
		var (contractId, playerId) = SeedPendingDelivery(map);
		const string pirateId = "pirate-flee";
		AddAmbientPirate(map, pirateId, new Coord(3, 0, 3));
		AssignInterceptor(map, contractId, playerId, pirateId);
		SetPendingEngagement(map, playerId, pirateId);
		var engine = CreateEngine(map, playerId, pirateId);
		Assert.True(EngagementQueries.TryGetPendingPlayerEngagement(
			map,
			playerId,
			out var pending));
		Assert.True(pending.FleeFailsDelivery);

		engine.Commit(new FleeAction(playerId));

		Assert.True(map.ContractRegistry.TryGetState(contractId, out var state));
		var delivery = Assert.IsType<DeliveryContractState>(state);
		Assert.Equal(EContractStatus.Failed, delivery.Status);
		Assert.Equal(EDeliveryFailureReason.FledInterceptor, delivery.Progress.FailureReason);
		Assert.Null(map.FleetRegistry.FleetOf(pirateId).State.PursuitDirective);
	}

	[Fact]
	public void FleeingUnrelatedPirate_DoesNotFailDelivery()
	{
		var map = maps.Fresh(5);
		var (contractId, playerId) = SeedPendingDelivery(map);
		const string pirateId = "pirate-unrelated";
		AddAmbientPirate(map, pirateId, new Coord(3, 0, 3));
		SetPendingEngagement(map, playerId, pirateId);
		var engine = CreateEngine(map, playerId, pirateId);

		engine.Commit(new FleeAction(playerId));

		Assert.True(map.ContractRegistry.TryGetState(contractId, out var state));
		Assert.Equal(EContractStatus.Active, state.Status);
	}

	[Fact]
	public void DestroyingAssignedInterceptor_ResolvesInterceptionAndKeepsDeliveryActive()
	{
		var map = maps.Fresh(5);
		var (contractId, playerId) = SeedPendingDelivery(map);
		const string pirateId = "pirate-defeated";
		AddAmbientPirate(map, pirateId, new Coord(3, 0, 3));
		AssignInterceptor(map, contractId, playerId, pirateId);
		var engagement = SetCommittedEngagement(map, playerId, pirateId);
		var pirate = map.FleetRegistry.FleetOf(pirateId);
		var player = map.FleetRegistry.FleetOf(playerId);
		var outcome = new BattleOutcome(
			engagement.Id,
			EBattleResult.Win,
			[
				..player.Members.Select(member => OutcomeTestKit.Handoff(
					member.Id,
					OutcomeTestKit.ChassisFromShipId(member.Id),
					1)),
				..pirate.Members.Select(member => OutcomeTestKit.Handoff(
					member.Id,
					OutcomeTestKit.ChassisFromShipId(member.Id),
					0)),
			]);
		var engine = CreateEngine(map, playerId, pirateId);

		engine.Commit(new ResolveEngagementAction(playerId, outcome, [], ResourceBundle.Empty));

		Assert.True(map.ContractRegistry.TryGetState(contractId, out var state));
		var delivery = Assert.IsType<DeliveryContractState>(state);
		Assert.Equal(EContractStatus.Active, delivery.Status);
		Assert.Equal(EDeliveryInterceptionState.Resolved, delivery.Progress.InterceptionState);
		Assert.False(map.FleetRegistry.Contains(pirateId));
	}

	[Fact]
	public void PartiallySurvivingInterceptor_RemainsAssigned()
	{
		var map = maps.Fresh(5);
		var (contractId, playerId) = SeedPendingDelivery(map);
		const string pirateId = "pirate-survives";
		const string survivorId = "pirate-survives.extra";
		AddAmbientPirate(map, pirateId, new Coord(3, 0, 3));
		var pirate = map.FleetRegistry.FleetOf(pirateId);
		map.FleetRegistry.Replace(new Fleet(
			pirate.State,
			[..pirate.Members, new FleetMember(survivorId)],
			pirate.Registrations));
		AssignInterceptor(map, contractId, playerId, pirateId);
		var engagement = SetCommittedEngagement(map, playerId, pirateId);
		pirate = map.FleetRegistry.FleetOf(pirateId);
		var destroyedId = pirate.Members[0].Id;
		var outcome = new BattleOutcome(
			engagement.Id,
			EBattleResult.Win,
			[
				OutcomeTestKit.Handoff(
					destroyedId,
					BattleUnitType.RepurposedMiner,
					0),
				OutcomeTestKit.Handoff(
					survivorId,
					BattleUnitType.RepurposedMiner,
					1),
			]);
		var engine = CreateEngine(map, playerId, pirateId);

		engine.Commit(new ResolveEngagementAction(playerId, outcome, [], ResourceBundle.Empty));

		var delivery = Assert.IsType<DeliveryContractState>(
			map.ContractRegistry.TryGetState(contractId, out var state) ? state : null);
		Assert.Equal(EDeliveryInterceptionState.Assigned, delivery.Progress.InterceptionState);
		Assert.True(map.FleetRegistry.Contains(pirateId));
		Assert.Single(map.FleetRegistry.FleetOf(pirateId).Members);
	}

	[Fact]
	public void EndContractEffect_CleansAndRestoresInterceptorState()
	{
		var map = maps.Fresh(5);
		var (contractId, playerId) = SeedPendingDelivery(map);
		const string pirateId = "pirate-cleanup";
		AddAmbientPatrolPirate(map, pirateId, new Coord(3, 0, 3));
		AssignInterceptor(map, contractId, playerId, pirateId);
		var player = map.FleetRegistry.FleetOf(playerId);
		player.State.TravelTarget = TravelTarget.Fleet(pirateId, EContactIntent.Engagement);
		new SetEngagementIntentEffect(pirateId, playerId)
			.Apply(map, new ActorRuntime(), pirateId);
		new SetTravelTargetEffect(
			pirateId,
			TravelTarget.Fleet(playerId, EContactIntent.Engagement))
			.Apply(map, new ActorRuntime(), pirateId);
		var effect = new EndContractEffect(contractId, EContractStatus.Completed);
		var runtime = new ActorRuntime();

		var records = effect.Apply(map, runtime, StarSystemActorIds.Contracts);

		Assert.Null(map.FleetRegistry.FleetOf(pirateId).State.PursuitDirective);
		Assert.Equal(TravelTarget.None, player.State.TravelTarget);
		Assert.Equal(TravelTarget.None, map.StateOf(pirateId).TravelTarget);
		Assert.Null(map.StateOf(pirateId).CurrentEngagement);
		Assert.Null(player.State.CurrentEngagement);
		Assert.Equal(
			new FleetPursuitChanged(pirateId, playerId, false),
			Assert.Single(records.OfType<Record<FleetPursuitChanged>>()).Value);
		Assert.Equal(
			new ContractStateChanged(contractId, playerId, EContractStatus.Completed),
			Assert.Single(records.OfType<Record<ContractStateChanged>>()).Value);
		Assert.True(map.Timeline.ContainsPending(action =>
			action is ReturnToPatrolAction patrol && patrol.ActorId == pirateId));

		effect.Undo(map, runtime, StarSystemActorIds.Contracts);

		Assert.True(map.ContractRegistry.TryGetState(contractId, out var restored));
		Assert.Equal(EContractStatus.Active, restored.Status);
		Assert.Equal(
			new FleetPursuitDirective(contractId, playerId),
			map.FleetRegistry.FleetOf(pirateId).State.PursuitDirective);
		Assert.Equal(
			TravelTarget.Fleet(pirateId, EContactIntent.Engagement),
			player.State.TravelTarget);
		Assert.Equal(
			TravelTarget.Fleet(playerId, EContactIntent.Engagement),
			map.StateOf(pirateId).TravelTarget);
		Assert.Equal(playerId, map.StateOf(pirateId).CurrentEngagement?.Hunting);
		Assert.Equal(pirateId, player.State.CurrentEngagement?.HuntedBy);
		Assert.False(map.Timeline.ContainsPending(action =>
			action is ReturnToPatrolAction patrol && patrol.ActorId == pirateId));
	}

	[Fact]
	public void SaveDtoMapper_RoundTripsAssignedDeliveryStateAndDirective()
	{
		var map = maps.Fresh(5);
		var (contractId, playerId) = SeedPendingDelivery(map);
		const string pirateId = "pirate-save";
		AddAmbientPirate(map, pirateId, new Coord(3, 0, 3));
		AssignInterceptor(map, contractId, playerId, pirateId);
		var registry = PersistenceRegistry.CreateDefault();

		var restored = SaveDtoMapper.RestoreStarMap(
			SaveDtoMapper.CaptureStarMap(map, registry),
			registry);

		var delivery = Assert.IsType<DeliveryContractState>(
			restored.ContractRegistry.TryGetState(contractId, out var state) ? state : null);
		Assert.Equal(EDeliveryInterceptionState.Assigned, delivery.Progress.InterceptionState);
		Assert.Equal(pirateId, delivery.Progress.InterceptorFleetId);
		Assert.Equal(
			new FleetPursuitDirective(contractId, playerId),
			restored.FleetRegistry.FleetOf(pirateId).State.PursuitDirective);
	}

	private static (string ContractId, string PlayerId) SeedPendingDelivery(
		StarMap map,
		string contractId = "delivery-interception",
		string? holderId = null)
	{
		var plan = map.Blueprint.SupplyPlan;
		var contract = ContractFactory.Build(
			map,
			contractId,
			EContractKind.Delivery,
			new DeliveryCreateArgs(
				plan.StoragePoiId,
				EDangerLevel.Moderate,
				ContractNarrative.ForDelivery("Delivery", "Pickup.", "Dropoff."),
				DropoffPoiId: plan.ExitPoiId,
				DropoffFacilityId: Facility.ScopedId(plan.ExitPoiId, Wormhole.TravelFacilitySlug),
				DropoffOperatorName: MapFacilityOperators.TravelOperatorName(map)));
		map.ContractRegistry.TryAdd(contract);
		var playerId = holderId ?? map.FleetRegistry.All.First().State.Id;
		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(playerId);
		runtimes.For(StarSystemActorIds.Contracts);
		var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);
		engine.Commit(ContractActionTestContext.AcceptDelivery(map, playerId, contractId));
		if (map.ContractRegistry.TryGetState(contractId, out var state)
			&& state is DeliveryContractState delivery
			&& delivery.Progress.InterceptionState != EDeliveryInterceptionState.Pending)
		{
			map.ContractRegistry.ReplaceState(delivery with
			{
				Progress = delivery.Progress with
				{
					InterceptionState = EDeliveryInterceptionState.Pending,
				},
			});
		}

		return (contractId, playerId);
	}

	private static Engine<StarMap, ActorRuntime> CreateEngine(
		StarMap map,
		params string[] actorIds)
	{
		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(StarSystemActorIds.Contracts);
		foreach (var actorId in actorIds)
			runtimes.For(actorId);
		return new Engine<StarMap, ActorRuntime>(map, runtimes);
	}

	private static void AssignInterceptor(
		StarMap map,
		string contractId,
		string playerId,
		string pirateId)
	{
		var state = Assert.IsType<DeliveryContractState>(
			map.ContractRegistry.TryGetState(contractId, out var contractState)
				? contractState
				: null);
		map.ContractRegistry.ReplaceState(state.WithInterceptorAssigned(pirateId));
		map.FleetRegistry.FleetOf(pirateId).State.PursuitDirective =
			new FleetPursuitDirective(contractId, playerId);
	}

	private static void SetPendingEngagement(StarMap map, string playerId, string pirateId)
	{
		const string engagementId = "delivery-flee";
		map.StateOf(playerId).CurrentEngagement = new StrategicEngagement(
			engagementId,
			EEngagementPhase.AwaitingDecision,
			pirateId,
			[],
			pirateId,
			null);
		map.StateOf(pirateId).CurrentEngagement = new StrategicEngagement(
			engagementId,
			EEngagementPhase.AwaitingDecision,
			pirateId,
			[],
			null,
			playerId);
	}

	private static StrategicEngagement SetCommittedEngagement(
		StarMap map,
		string playerId,
		string pirateId)
	{
		var engagement = new StrategicEngagement(
			"delivery-victory",
			EEngagementPhase.Engaged,
			pirateId,
			new HashSet<string>([playerId, pirateId], StringComparer.Ordinal),
			null,
			null);
		map.StateOf(playerId).CurrentEngagement = engagement;
		map.StateOf(pirateId).CurrentEngagement = engagement;
		return engagement;
	}

	private static void AddAmbientPirate(StarMap map, string id, Coord coord)
	{
		var pirate = StarSystemTestHarness.CreatePirateFleet(id, coord, map.ControllingFaction);
		pirate.State.SpawnerSource = EFleetSpawnerSource.RandomArea;
		map.FleetRegistry.Add(pirate);
	}

	private static void AddAmbientPatrolPirate(StarMap map, string id, Coord coord)
	{
		var pirate = GrimSpace.World.StarSystem.Units.Factory.Create(
			new Spawn(
				id,
				EType.PirateFleet,
				coord,
				UnitDefaults.PatrolSpeedPerTick(EType.PirateFleet),
				UnitDefaults.EngageRadius(EType.PirateFleet),
				UnitDefaults.VisionRadius(EType.PirateFleet),
				[],
				map.ControllingFaction,
				PatrolRadius: RandomAreaFleetSpawnRecipe.PatrolRadius),
			[BattleUnitType.RepurposedMiner]);
		pirate.State.SpawnerSource = EFleetSpawnerSource.RandomArea;
		map.FleetRegistry.Add(pirate);
	}

	private static void SetTraveling(StarMap map, string unitId)
	{
		var state = map.StateOf(unitId);
		var origin = state.AtRest().Position;
		var destination = map.DocksByPoiId[map.Blueprint.SupplyPlan.ExitPoiId].Position;
		state.StartJourney(1, origin, destination, map.Timeline.Clock.Current);
	}

}
