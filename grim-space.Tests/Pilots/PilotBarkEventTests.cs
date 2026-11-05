using GrimSpace.Pilots.Barks;
using GrimSpace.Core.Log;
using GrimSpace.Run;
using GrimSpace.Tests.World.StarSystem.Contracts;
using GrimSpace.Tutorials;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.World.StarSystem.Traffic;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.Units;
using RunState = GrimSpace.Run.State;
using State = GrimSpace.Run.State;

namespace GrimSpace.Tests.Pilots;

[IntegrationTestSuite]
public sealed class PilotBarkEventTests
{
	[Theory]
	[InlineData(true, 0, EBarkFlavor.Scared)]
	[InlineData(true, 1, EBarkFlavor.Angry)]
	[InlineData(true, 2, EBarkFlavor.Surprised)]
	[InlineData(false, 0, EBarkFlavor.Relieved)]
	[InlineData(false, 1, EBarkFlavor.Bored)]
	public void WreckReached_UsesVisibleOutcomeFlavorPool(bool ambush, int pick, EBarkFlavor expected)
	{
		using var run = RunState.CreateNewRun(42);
		run.TryConsumeOpening();
		using var director = new PilotBarkDirector(run, new WreckRandom(pick));
		var lines = new List<string>();
		director.RegisterPilot(run.PilotRegistry.Active[0].Id, line =>
		{
			lines.Add(line);
			return true;
		});
		var map = run.StarSystem.Map;
		var position = map.StateOf(RunState.PlayerFleetUnitId)
			.PositionAt(map, null, 0).Position;
		WreckageOutcome outcome = ambush
			? new WreckageOutcome.Ambush(new FleetSpawnSpec(
				EType.PirateFleet, EFaction.Pirates, 9,
				[(GrimSpace.Units.Enums.EType.RepurposedMiner, GrimSpace.Units.Enums.EShipGearTier.T0)]))
			: new WreckageOutcome.Salvage(ResourceBundle.Empty);
		var poiId = map.Blueprint.SupplyPlan.AdministrativePoiId;
		var contract = new Contract(
			"bark-wreck",
			new WreckageObjective("bark-wreck.site",
				new AreaPick(new AreaIntel("Near {A}.", poiId, poiId, poiId), [position]), outcome),
			EDangerLevel.VeryLow, map.ControllingFaction,
			new ContractTerms(ResourceBundle.Empty), new ContractNarrative("Wreck", "Investigate."));
		Assert.True(map.ContractRegistry.TryAdd(contract));
		run.StarSystem.CommitSetup(new AcceptContractAction(RunState.PlayerFleetUnitId, contract.Id));
		Assert.Empty(lines);

		run.StarSystem.CommitSetup(new ReachWreckageAction(RunState.PlayerFleetUnitId, contract.Id));

		Assert.True(WreckageQueries.TryGetPendingPlayerWreckDecision(
			map, RunState.PlayerFleetUnitId, out var pending));
		Assert.Equal(ambush, pending.IsAmbush);
		Assert.Contains(Assert.Single(lines), PilotBarkCatalog.LinesFor(expected));
	}

	private sealed class WreckRandom(int pick) : Random
	{
		public override double NextDouble() => 0;
		public override int Next(int maxValue) => System.Math.Min(pick, maxValue - 1);
	}

	[Fact]
	public void Travel_OffersOnceInsideJourney_NotFromElapsedRealTime()
	{
		using var run = RunState.CreateNewRun(42);
		run.TryConsumeOpening();
		using var director = new PilotBarkDirector(run, new AlwaysSpeakRandom());
		var lines = new List<string>();
		director.RegisterPilot(run.PilotRegistry.Active[0].Id, line =>
		{
			lines.Add(line);
			return true;
		});
		StartTravel(run);
		director.Tick(100);
		Assert.Empty(lines);

		run.StarSystem.Map.Timeline.Clock.Set(run.StarSystem.Tick + 1);
		director.Tick(0);
		director.Tick(100);

		Assert.Contains(Assert.Single(lines), PilotBarkCatalog.LinesFor(EBarkFlavor.Bored));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Travel_CancelsOnArrivalOrInterruption(bool interrupted)
	{
		using var run = RunState.CreateNewRun(42);
		run.TryConsumeOpening();
		using var director = new PilotBarkDirector(run, new AlwaysSpeakRandom());
		director.RegisterPilot(run.PilotRegistry.Active[0].Id,
			_ => throw new InvalidOperationException("Cancelled journey spoke."));
		StartTravel(run);
		var map = run.StarSystem.Map;
		var fleet = map.StateOf(RunState.PlayerFleetUnitId);
		if (interrupted)
			fleet.CurrentEngagement = new GrimSpace.World.StarSystem.Units.Engagement(
				"interrupt", EEngagementPhase.Pursuing, RunState.PlayerFleetUnitId,
				new HashSet<string>(), HuntedBy: null, Hunting: "hostile");
		else
			fleet.StopAt(default);

		director.Tick(1);
		fleet.CurrentEngagement = null;
		map.Timeline.Clock.Set(run.StarSystem.Tick + 10);
		director.Tick(100);
	}

	[Fact]
	public void Travel_NewMoveReplacesPendingAttempt()
	{
		using var run = RunState.CreateNewRun(42);
		run.TryConsumeOpening();
		using var director = new PilotBarkDirector(run, new AlwaysSpeakRandom());
		var lines = new List<string>();
		director.RegisterPilot(run.PilotRegistry.Active[0].Id, line =>
		{
			lines.Add(line);
			return true;
		});
		StartTravel(run);
		var map = run.StarSystem.Map;
		map.Timeline.Clock.Set(run.StarSystem.Tick + 1);
		StartTravel(run);
		director.Tick(0);
		Assert.Empty(lines);

		map.Timeline.Clock.Set(run.StarSystem.Tick + 1);
		director.Tick(0);

		Assert.Single(lines);
	}

	private static void StartTravel(RunState run)
	{
		var map = run.StarSystem.Map;
		var origin = map.StateOf(RunState.PlayerFleetUnitId)
			.PositionAt(map, run.StarSystem.RuntimeFor(RunState.PlayerFleetUnitId).CachedPath, 0).Position;
		var destination = new Coord(origin.X + 1000, origin.Y, origin.Z);
		var path = TransitPath.FromPoints([origin, destination], [1.0, 1.0]);
		run.StarSystem.CommitSetup(new MoveAction(
			RunState.PlayerFleetUnitId, RunState.PlayerFleetUnitId, destination, path));
	}

	[Fact]
	public void ContactReached_BetweenOtherFleets_DoesNotBark()
	{
		using var run = RunState.CreateNewRun(42);
		using var director = new PilotBarkDirector(run, new AlwaysSpeakRandom());
		director.RegisterPilot(run.PilotRegistry.Active[0].Id,
			_ => throw new InvalidOperationException("Unrelated contact triggered a bark."));
		var map = run.StarSystem.Map;
		foreach (var id in new[] { "bark-pirate-a", "bark-pirate-b" })
			map.FleetRegistry.Add(StarSystemTestHarness.CreatePirateFleet(
				id, new Coord(4, 0, 0), EFaction.Pirates));
		new SetEngagementIntentEffect("bark-pirate-a", "bark-pirate-b")
			.Apply(map, new ActorRuntime(), "bark-pirate-a");
		new SetTravelTargetEffect("bark-pirate-a",
			TravelTarget.Fleet("bark-pirate-b", EContactIntent.Engagement))
			.Apply(map, new ActorRuntime(), "bark-pirate-a");

		run.StarSystem.CommitSetup(new ReachContactAction("bark-pirate-a", "bark-pirate-b"));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ContactReached_PlayerDecision_OffersEngagementFlavor(bool playerInitiates)
	{
		using var run = RunState.CreateNewRun(42);
		run.TryConsumeOpening();
		using var director = new PilotBarkDirector(run, new AlwaysSpeakRandom());
		var lines = new List<string>();
		director.RegisterPilot(run.PilotRegistry.Active[0].Id, line =>
		{
			lines.Add(line);
			return true;
		});
		var map = run.StarSystem.Map;
		map.FleetRegistry.Add(StarSystemTestHarness.CreatePirateFleet(
			"bark-pirate", new Coord(4, 0, 0), EFaction.Pirates));
		var initiator = playerInitiates ? RunState.PlayerFleetUnitId : "bark-pirate";
		var target = playerInitiates ? "bark-pirate" : RunState.PlayerFleetUnitId;
		new SetEngagementIntentEffect(initiator, target).Apply(map, new ActorRuntime(), initiator);
		new SetTravelTargetEffect(initiator, TravelTarget.Fleet(target, EContactIntent.Engagement))
			.Apply(map, new ActorRuntime(), initiator);

		run.StarSystem.CommitSetup(new ReachContactAction(initiator, target));

		Assert.True(EngagementQueries.TryGetPendingPlayerEngagement(
			map, RunState.PlayerFleetUnitId, out _));
		Assert.Contains(Assert.Single(lines), PilotBarkCatalog.LinesFor(EBarkFlavor.Scared));
	}

	[Fact]
	public void RunDirector_KeepsCooldownsWhenSceneHandlesChange()
	{
		using var run = State.CreateNewRun(42);
		run.TryConsumeOpening();
		var director = run.Barks;
		var pilotId = run.PilotRegistry.Active[0].Id;
		director.RegisterPilot(pilotId, _ => true);
		Assert.True(director.Offer(pilotId, EBarkFlavor.Relieved, 1));
		director.UnregisterPilot(pilotId);
		director.Tick(1);
		director.RegisterPilot(pilotId, _ => true);

		Assert.Same(director, run.Barks);
		Assert.False(director.Offer(pilotId, EBarkFlavor.Relieved, 1));
		director.Tick(PilotBarkDirector.DefaultPilotCooldownSeconds);
		Assert.True(director.Offer(pilotId, EBarkFlavor.Relieved, 1));
	}

	[Fact]
	public void RunDirector_DoesNotConsumeOpeningBeforeHandlesRegister()
	{
		using var run = State.CreateNewRun(42);
		run.Barks.Tick(1);
		var lines = new List<string>();
		run.Barks.RegisterPilot(run.PilotRegistry.Active[0].Id, line =>
		{
			lines.Add(line);
			return true;
		});

		run.Barks.Tick(1);

		Assert.Equal(PilotBarkCatalog.OpeningLines, lines);
	}

	[Fact]
	public void RunDirector_RebindsAfterMapRegeneration()
	{
		using var run = State.CreateNewRun(42);
		var director = run.Barks;

		run.RegenerateMap(43);
		var logs = new List<string>();
		using var scope = GameLog.BeginScope(logs.Add);
		CompleteDelivery(run);

		Assert.Same(director, run.Barks);
		Assert.Single(logs, line => line == "[Barks] Ignore: no candidate pilots.");
	}

	[Fact]
	public void DeliveryCompletion_SpeaksThroughRegisteredHandle()
	{
		using var run = State.CreateNewRun(42);
		run.TryConsumeOpening();
		using var director = new PilotBarkDirector(run, new AlwaysSpeakRandom());
		var lines = new List<string>();
		director.RegisterPilot(run.PilotRegistry.Active[0].Id, line =>
		{
			lines.Add(line);
			return true;
		});

		CompleteDelivery(run);

		Assert.Contains(Assert.Single(lines),
			PilotBarkCatalog.LinesFor(EBarkFlavor.Disdain));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void RemovedHandleOrDisposedDirector_DoesNotSpeak(bool dispose)
	{
		using var run = State.CreateNewRun(42);
		run.TryConsumeOpening();
		using var director = new PilotBarkDirector(run, new AlwaysSpeakRandom());
		var pilotId = run.PilotRegistry.Active[0].Id;
		director.RegisterPilot(pilotId, _ => throw new InvalidOperationException("Stale handle called."));
		if (dispose)
			director.Dispose();
		else
			director.UnregisterPilot(pilotId);

		CompleteDelivery(run);
	}

	[Fact]
	public void AcceptingDelivery_DoesNotSpeak()
	{
		using var run = State.CreateNewRun(42);
		run.TryConsumeOpening();
		using var director = new PilotBarkDirector(run, new AlwaysSpeakRandom());
		director.RegisterPilot(run.PilotRegistry.Active[0].Id,
			_ => throw new InvalidOperationException("Acceptance must not trigger a bark."));

		var contractId = TutorialBeatContracts.OfferBeatB(run.StarSystem.Map);
		run.StarSystem.CommitSetup(new AcceptContractAction(State.PlayerFleetUnitId, contractId));

		Assert.False(run.StarSystem.Map.ContractRegistry.IsCompleted(contractId));
	}

	[Fact]
	public void UnregisteredPilot_CanRegisterANewHandle()
	{
		using var run = State.CreateNewRun(42);
		run.TryConsumeOpening();
		using var director = new PilotBarkDirector(run, new AlwaysSpeakRandom());
		var pilotId = run.PilotRegistry.Active[0].Id;
		director.RegisterPilot(pilotId, _ => throw new InvalidOperationException("Old handle called."));
		director.UnregisterPilot(pilotId);
		var spoken = false;
		director.RegisterPilot(pilotId, _ => spoken = true);

		CompleteDelivery(run);

		Assert.True(spoken);
	}

	[Fact]
	public void RegisteredHandle_ForNonActivePilot_IsNotSelected()
	{
		using var run = State.CreateNewRun(42);
		run.TryConsumeOpening();
		using var director = new PilotBarkDirector(run, new AlwaysSpeakRandom());
		director.RegisterPilot("not-an-active-pilot",
			_ => throw new InvalidOperationException("Inactive pilot selected."));

		CompleteDelivery(run);
	}

	[Fact]
	public void Tick_PlaysOpeningOnceThroughRegisteredHandle()
	{
		using var run = State.CreateNewRun(42);
		using var director = new PilotBarkDirector(run, new AlwaysSpeakRandom());
		var lines = new List<string>();
		director.RegisterPilot(run.PilotRegistry.Active[0].Id, line =>
		{
			lines.Add(line);
			return true;
		});

		director.Tick(0);
		director.Tick(1);

		Assert.Equal(PilotBarkCatalog.OpeningLines, lines);
	}

	private static void CompleteDelivery(State run)
	{
		var map = run.StarSystem.Map;
		var contractId = TutorialBeatContracts.OfferBeatB(map);
		run.StarSystem.CommitSetup(
			new AcceptContractAction(State.PlayerFleetUnitId, contractId));
		run.StarSystem.CommitSetup(
			ContractActionTestContext.TurnInDelivery(map, State.PlayerFleetUnitId, contractId));
		Assert.True(map.ContractRegistry.IsCompleted(contractId));
	}

	private sealed class AlwaysSpeakRandom : Random
	{
		public override double NextDouble() => 0;
		public override int Next(int maxValue) => 0;
	}
}
