using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Objectives;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem.Traffic;
using GrimSpace.Tutorials;

namespace GrimSpace.Tests.World.StarSystem;

[StarSystemTestSuite]
public sealed class StarMapTests
{
	[Fact]
	public void Create_HasFiveNonOverlappingInBoundsPois()
	{
		var world = StarMap.Create(42);

		Assert.Equal(42, world.Seed);
		Assert.Equal(StarMap.MapWidth, world.Width);
		Assert.Equal(StarMap.MapHeight, world.Height);
		Assert.Equal(7, world.PointsOfInterest.Count);
		Assert.Equal(EStarSystemClass.Supply, world.Blueprint.SystemClass);

		Assert.Contains(world.PointsOfInterest, poi => poi is Star);
		Assert.Contains(world.PointsOfInterest, poi => poi is OreMine);
		Assert.Contains(world.PointsOfInterest, poi => poi is Refinery);
		Assert.Contains(world.PointsOfInterest, poi => poi is StorageFacility);
		Assert.Contains(world.PointsOfInterest, poi => poi is Wormhole);
		Assert.Contains(world.PointsOfInterest, poi => poi is AdministrativeCore);
		Assert.Contains(world.PointsOfInterest, poi => poi is TradeHub);

		for (var i = 0; i < world.PointsOfInterest.Count; i++)
		{
			for (var j = i + 1; j < world.PointsOfInterest.Count; j++)
				Assert.False(
					StarMap.PoisOverlap(world.PointsOfInterest[i], world.PointsOfInterest[j]),
					$"POIs overlap: {world.PointsOfInterest[i].Id} and {world.PointsOfInterest[j].Id}");
		}

		foreach (var poi in world.PointsOfInterest)
		{
			Assert.True(world.IsInBounds(poi.PlacedCenter), $"Center out of bounds: {poi.PlacedCenter}");
			Assert.True(poi.Radius > 0);

			Assert.True(GridBounds.IsCircleWhollyInRectangle(poi.PlacedCenter, poi.Radius, world.Width, world.Height),
				$"Radius extends out of bounds for {poi.Id}");
		}
	}

	[Fact]
	public void IsInBounds_AcceptsMaxPoint_RejectsOverflow()
	{
		var world = StarMap.Create();

		Assert.True(world.IsInBounds(new Coord(1023, 0, 1023)));
		Assert.False(world.IsInBounds(new Coord(1024, 0, 0)));
		Assert.False(world.IsInBounds(new Coord(0, 1, 0)));
	}

	[Fact]
	public void Fork_PreservesStateAndIndependentsTimeline()
	{
		var world = StarMap.Create(7);
		world.Timeline.Clock.Set(3);

		var fork = world.Fork();

		Assert.Equal(world.Seed, fork.Seed);
		Assert.Equal(world.Width, fork.Width);
		Assert.Equal(world.Height, fork.Height);
		Assert.Same(world.Blueprint, fork.Blueprint);
		Assert.NotSame(world.PointsOfInterest, fork.PointsOfInterest);
		Assert.Same(world.DocksById, fork.DocksById);
		Assert.Same(world.DocksByPoiId, fork.DocksByPoiId);
		Assert.Same(world.RoutesById, fork.RoutesById);
		Assert.Same(world.PathfindingTerrain, fork.PathfindingTerrain);
		Assert.NotSame(world.Timeline, fork.Timeline);
		Assert.NotSame(world.FleetRegistry, fork.FleetRegistry);
		Assert.NotSame(world.PlayerResources, fork.PlayerResources);
		Assert.Equal(3, fork.Timeline.Clock.Current);
		Assert.Equal(26, world.FleetRegistry.Ids.Count());
		Assert.Equal(26, fork.FleetRegistry.Ids.Count());

		fork.Timeline.Clock.Set(9);
		fork.FleetRegistry.FleetOf(FirstUnitOfType(world, EType.MiningBarge).Id).State.Journey.StartTick = 99;
		Assert.Equal(3, world.Timeline.Clock.Current);
		Assert.Equal(9, fork.Timeline.Clock.Current);
		var minerId = FirstUnitOfType(world, EType.MiningBarge).Id;
		Assert.NotEqual(
			world.FleetRegistry.FleetOf(minerId).State.Journey.StartTick,
			fork.FleetRegistry.FleetOf(minerId).State.Journey.StartTick);

		new PlayerInputEffect(true).Apply(world, new ActorRuntime(), "actor");
		var waitingFork = world.Fork();
		Assert.True(waitingFork.WaitingForPlayerInput);
		new PlayerInputEffect(false).Apply(waitingFork, new ActorRuntime(), "actor");
		Assert.True(world.WaitingForPlayerInput);
	}

	[Fact]
	public void Create_HasFiveDocks_NoStarDock()
	{
		var world = StarMap.Create(0);

		Assert.Equal(6, world.DocksById.Count);
		Assert.Equal(26, world.FleetRegistry.Ids.Count());
		Assert.DoesNotContain(
			world.PointsOfInterest.First(p => p is Star).Id,
			world.DocksByPoiId.Keys);
	}

	[Fact]
	public void Rehydrate_PreservesInTransitJourneyAndCompletesPendingMove()
	{
		const string playerFleetId = "player-fleet";
		var map = StarMap.Create(42);
		StarSystemTestHarness.AddPlayerFleet(map, playerFleetId);
		using var original = StarSystemOrchestrator.FromMap(map, playerFleetId);

		var fleet = map.FleetRegistry.FleetOf(playerFleetId);
		var origin = map.DocksById[fleet.State.DockedAtDockId].Position;
		var destination = origin + new Coord(1, 0, 0);
		var path = TransitPath.FromPoints([origin, destination], [1.0, 1.0]);

		original.CommitSetup(new MoveAction(playerFleetId, playerFleetId, destination, path));

		Assert.Equal(EPhase.InTransit, fleet.State.Phase);
		Assert.Equal(destination, fleet.State.Journey.Destination);
		Assert.True(map.Timeline.ContainsPending(
			action => action is CompleteMoveAction complete && complete.UnitId == playerFleetId));

		original.Dispose();
		using var rehydrated = StarSystemOrchestrator.FromMap(map, playerFleetId);
		var restoredFleet = map.FleetRegistry.FleetOf(playerFleetId);

		Assert.Equal(EPhase.InTransit, restoredFleet.State.Phase);
		Assert.Equal(destination, restoredFleet.State.Journey.Destination);

		for (var i = 0; i < 100 && restoredFleet.State.Phase == EPhase.InTransit; i++)
			rehydrated.AdvanceTick();

		Assert.Equal(EPhase.Docked, restoredFleet.State.Phase);
		Assert.Equal(destination, restoredFleet.State.IdleCoord);
		Assert.False(map.Timeline.ContainsPending(
			action => action is CompleteMoveAction complete && complete.UnitId == playerFleetId));
	}

	[Fact]
	public void Fork_PreservesCompleteAuthoritativeMapState()
	{
		var world = StarMap.Create(42);
		world.Timeline.Clock.Set(7);
		world.WaitingForPlayerInput = true;
		world.ActiveNarrativeId = "opening";
		world.PointsOfInterest[0].NextAvailableTaskTick = 19;
		world.PlayerResources.TryApply(ResourceBundle.Create(
			(ResourceId.Credits, 120),
			(ResourceId.ScrapAlloy, 7)));
		var storyContractId = TutorialBeatContracts.OfferBeatA(world);
		world.StoryObjectives.Add(StoryObjective.FirstContract(
			world.Blueprint.SupplyPlan.AdministrativePoiId));

		var copy = world.Fork();

		Assert.Equal(world.Seed, copy.Seed);
		Assert.Equal(world.Blueprint, copy.Blueprint);
		Assert.Equal(world.WaitingForPlayerInput, copy.WaitingForPlayerInput);
		Assert.Equal(world.ActiveNarrativeId, copy.ActiveNarrativeId);
		Assert.Equal(world.Timeline.Clock.Current, copy.Timeline.Clock.Current);
		Assert.Equal(world.Timeline.History(), copy.Timeline.History());
		Assert.Equal(
			world.PointsOfInterest.Select(poi => (
				poi.Id,
				poi.DisplayName,
				poi.Center,
				poi.Radius,
				poi.LogicalRole,
				poi.Facade,
				poi.NextAvailableTaskTick)),
			copy.PointsOfInterest.Select(poi => (
				poi.Id,
				poi.DisplayName,
				poi.Center,
				poi.Radius,
				poi.LogicalRole,
				poi.Facade,
				poi.NextAvailableTaskTick)));
		Assert.Equal(
			world.NavigationLandmarks,
			copy.NavigationLandmarks);
		Assert.Equal(world.DocksById, copy.DocksById);
		Assert.Equal(world.RoutesById, copy.RoutesById);
		Assert.Equal(
			world.FleetRegistry.All.Select(FleetSnapshot),
			copy.FleetRegistry.All.Select(FleetSnapshot));
		Assert.Equal(
			world.PlayerResources.EnumerateBalances(),
			copy.PlayerResources.EnumerateBalances());
		Assert.Equal(
			world.ContractRegistry.All.Select(contract => contract.Id),
			copy.ContractRegistry.All.Select(contract => contract.Id));
		Assert.True(copy.ContractRegistry.Contains(storyContractId!));
		Assert.Equal(
			world.StoryObjectives.Active.Select(objective => objective.Id),
			copy.StoryObjectives.Active.Select(objective => objective.Id));
	}

	private static State FirstUnitOfType(StarMap map, EType type) =>
		map.FleetRegistry.All.First(unit => unit.State.Type == type).State;

	private static object FleetSnapshot(Fleet fleet) =>
		(
			fleet.State.Id,
			fleet.State.Type,
			fleet.State.Faction,
			fleet.State.DockedAtDockId,
			fleet.State.IdleCoord,
			fleet.State.Phase,
			fleet.State.ChoreDockIds,
			fleet.State.ChoreIndex,
			fleet.State.SpeedPerTick,
			fleet.State.EngageRadius,
			fleet.State.VisionRadius,
			fleet.State.WorkStartTick,
			fleet.State.CurrentEngagement,
			fleet.State.TravelTarget,
			fleet.State.PendingWreckContractId,
			fleet.State.Journey.JourneyId,
			fleet.State.Journey.Origin,
			fleet.State.Journey.Destination,
			fleet.State.Journey.StartTick,
			fleet.Members,
			fleet.Registrations);
}
