using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem.Traffic;
using RunState = GrimSpace.Run.State;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Engagement;

[StarSystemTestSuite]
public sealed class ContactMonitorTests(StarMapFixture maps)
{
	[Fact]
	public void Contact_ProducesAwaitingDecision()
	{
		var orchestrator = CreateOverlappingScenario();
		orchestrator.SetRunning();
		orchestrator.AdvanceTick();

		Assert.Equal(EEngagementPhase.AwaitingDecision,
			EngagementAssertions.Phase(orchestrator.Map.StateOf(RunState.PlayerFleetUnitId)));
		Assert.True(orchestrator.Map.WaitingForPlayerInput);
		Assert.False(orchestrator.CanAdvance);
	}

	[Fact]
	public void Contact_DoesNotDuplicateWhileAwaitingDecision()
	{
		var orchestrator = CreateOverlappingScenario();
		orchestrator.SetRunning();
		orchestrator.AdvanceTick();
		Assert.Equal(EEngagementPhase.AwaitingDecision,
			EngagementAssertions.Phase(orchestrator.Map.StateOf(RunState.PlayerFleetUnitId)));

		orchestrator.AdvanceTick();

		Assert.Equal(EEngagementPhase.AwaitingDecision,
			EngagementAssertions.Phase(orchestrator.Map.StateOf(RunState.PlayerFleetUnitId)));
	}

	[Fact]
	public void DeliveryMeetingContact_ProducesReachedContact()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var player = map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		player.State.Travel = new FleetTravel.AtRest(new Coord(0, 0, 0));
		const string meetingId = "delivery-meeting";
		map.FleetRegistry.Add(StarSystemTestHarness.CreatePirateFleet(
			meetingId,
			new Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.TheOptimality));
		new SetTravelTargetEffect(
			RunState.PlayerFleetUnitId,
			TravelTarget.Fleet(meetingId, EContactIntent.DeliveryMeeting))
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);
		var engine = new Engine<StarMap, ActorRuntime>(
			map,
			new ActorRuntimes<ActorRuntime>());
		var monitor = new ContactMonitor(engine, new StraightLinePathfinder());

		var contact = Assert.Single(monitor.Update(0));

		Assert.Equal(RunState.PlayerFleetUnitId, contact.ActorId);
		Assert.Equal(new FleetContactTarget(meetingId), contact.Target);
		Assert.Equal(EContactIntent.DeliveryMeeting, contact.Intent);
	}

	[Fact]
	public void FleeAction_ClearsHuntIntentAndResumesPlayback()
	{
		var orchestrator = CreateOverlappingScenario();
		orchestrator.SetStepped();
		orchestrator.Step();
		Assert.False(orchestrator.CanAdvance);

		var pirateId = orchestrator.Map.FleetRegistry.All
			.Single(unit => unit.State.Type == EType.PirateFleet)
			.State.Id;
		orchestrator.PlayerAgent!.TryEnqueue([new FleeAction(RunState.PlayerFleetUnitId)]);
		orchestrator.AdvanceClock();

		Assert.False(orchestrator.Map.WaitingForPlayerInput);
		Assert.Equal(ESimMode.Stepped, orchestrator.SimMode);
		Assert.Null(EngagementAssertions.Hunting(orchestrator.Map.StateOf(RunState.PlayerFleetUnitId)));
		Assert.Null(EngagementAssertions.HuntedBy(orchestrator.Map.StateOf(pirateId)));
	}

	[Fact]
	public void EnemyContact_PreservesDecisionUntilPlayerActs()
	{
		var orchestrator = CreateOverlappingEnemyScenario();
		orchestrator.SetRunning();

		orchestrator.AdvanceTick();

		Assert.Equal(
			EEngagementPhase.AwaitingDecision,
			EngagementAssertions.Phase(orchestrator.Map.StateOf(RunState.PlayerFleetUnitId)));
		Assert.True(orchestrator.Map.WaitingForPlayerInput);
		Assert.True(orchestrator.PlayerAgent!.TryEnqueue(
			[new EngageAction(RunState.PlayerFleetUnitId)]));
	}

	[Fact]
	public void EnemyContact_FleeBlocksCounterpartyActionsForTenTicks()
	{
		var orchestrator = CreateOverlappingEnemyScenario();
		orchestrator.SetRunning();
		orchestrator.AdvanceTick();
		var pirate = orchestrator.Map.FleetRegistry.All.Single(
			unit => unit.State.Type == EType.PirateFleet);

		Assert.True(orchestrator.PlayerAgent!.TryEnqueue(
			[new FleeAction(RunState.PlayerFleetUnitId)]));

		var cooldownStartTick = orchestrator.Tick;
		Assert.Equal(
			cooldownStartTick + 10,
			orchestrator.RuntimeFor(pirate.State.Id).ActionCooldownUntilTick);
		Assert.Null(orchestrator.Map.StateOf(RunState.PlayerFleetUnitId).CurrentEngagement);
		Assert.Null(pirate.State.CurrentEngagement);
		Assert.IsType<FleetTravel.AtRest>(pirate.State.Travel);

		for (var tick = 1; tick < 10; tick++)
		{
			orchestrator.AdvanceTick();
			Assert.IsType<FleetTravel.AtRest>(pirate.State.Travel);
		}

		orchestrator.AdvanceTick();

		Assert.Equal(cooldownStartTick + 10, orchestrator.Tick);
		Assert.IsType<FleetTravel.Journey>(pirate.State.Travel);
	}

	[Fact]
	public void DockedTarget_StopsPursuitAndIsReacquiredAfterDeparture()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var player = map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var dock = map.DockAt(player.State)!;
		const string pirateId = "pirate-dock-pursuit";
		var pirate = Factory.Create(
			new Spawn(
				pirateId,
				EType.PirateFleet,
				dock.Position + new Coord(4, 0, 0),
				UnitDefaults.SpeedPerTick(EType.PirateFleet),
				UnitDefaults.EngageRadius(EType.PirateFleet),
				UnitDefaults.VisionRadius(EType.PirateFleet),
				[],
				GrimSpace.World.Factions.EFaction.Pirates,
				PatrolRadius: 8,
				AggressionRating: 10),
			[GrimSpace.Units.Enums.EType.RepurposedMiner]);
		map.FleetRegistry.Add(pirate);
		new SetEngagementIntentEffect(pirateId, RunState.PlayerFleetUnitId)
			.Apply(map, new ActorRuntime(), pirateId);
		new SetTravelTargetEffect(
			pirateId,
			TravelTarget.Fleet(RunState.PlayerFleetUnitId, EContactIntent.Engagement))
			.Apply(map, new ActorRuntime(), pirateId);
		var orchestrator = StarSystemTestHarness.CreatePlayerOrchestrator(
			maps,
			RunState.PlayerFleetUnitId,
			42,
			map: map);
		orchestrator.SetRunning();

		orchestrator.AdvanceTick();

		Assert.Null(pirate.State.CurrentEngagement);
		Assert.Null(player.State.CurrentEngagement);
		Assert.Contains(
			map.Timeline.History(),
			entry => entry is StopPursuingDockedTargetAction stop
				&& stop.ActorId == pirateId);
		Assert.False(map.WaitingForPlayerInput);

		Assert.IsType<CourseCommandResult.Queued>(
			orchestrator.PlayerAgent!.TryQueueMove(dock.Position + new Coord(20, 0, 0)));
		orchestrator.AdvanceTick();

		Assert.Null(map.DockAt(player.State));
		Assert.Equal(
			RunState.PlayerFleetUnitId,
			EngagementAssertions.Hunting(pirate.State));
		Assert.IsType<FleetTravel.Journey>(pirate.State.Travel);
	}

	private StarSystemOrchestrator CreateOverlappingScenario()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var player = map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		player.State.Travel = new FleetTravel.AtRest(new Coord(0, 0, 0));
		var pirateId = "pirate-contact";
		map.FleetRegistry.Add(StarSystemTestHarness.CreatePirateFleet(
			pirateId,
			new Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.Pirates));
		new SetEngagementIntentEffect(RunState.PlayerFleetUnitId, pirateId)
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);
		new SetTravelTargetEffect(
			RunState.PlayerFleetUnitId,
			TravelTarget.Fleet(pirateId, EContactIntent.Engagement))
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);

		return StarSystemTestHarness.CreatePlayerOrchestrator(maps, RunState.PlayerFleetUnitId, 42, map: map);
	}

	private StarSystemOrchestrator CreateOverlappingEnemyScenario()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var player = map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		player.State.Travel = new FleetTravel.AtRest(new Coord(0, 0, 0));

		const string pirateId = "pirate-contact";
		map.FleetRegistry.Add(Factory.Create(
			new Spawn(
				pirateId,
				EType.PirateFleet,
				new Coord(4, 0, 0),
				UnitDefaults.SpeedPerTick(EType.PirateFleet),
				UnitDefaults.EngageRadius(EType.PirateFleet),
				UnitDefaults.VisionRadius(EType.PirateFleet),
				[],
				GrimSpace.World.Factions.EFaction.Pirates,
				PatrolRadius: 8),
			[GrimSpace.Units.Enums.EType.RepurposedMiner]));
		new SetEngagementIntentEffect(pirateId, RunState.PlayerFleetUnitId)
			.Apply(map, new ActorRuntime(), pirateId);
		new SetTravelTargetEffect(
			pirateId,
			TravelTarget.Fleet(RunState.PlayerFleetUnitId, EContactIntent.Engagement))
			.Apply(map, new ActorRuntime(), pirateId);

		return StarSystemTestHarness.CreatePlayerOrchestrator(
			maps,
			RunState.PlayerFleetUnitId,
			42,
			map: map);
	}

	private sealed class StraightLinePathfinder : IPathfinder
	{
		public PathfindingResult FindPath(Coord origin, Coord destination) =>
			new PathfindingResult.Found(
				TransitPath.FromPoints([origin, destination], [1.0, 1.0]));
	}
}
