using System.Reflection;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem.Traffic;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.World.StarSystem.PlayerFleet;

public sealed class PlayerFleetMovementTests
{
	[Fact]
	public void OrderMove_QueuesMoveWithoutMutatingLiveMap()
	{
		var orchestrator = CreatePlayerOrchestrator(42);
		var map = orchestrator.Map;
		var player = map.UnitRegistry.UnitOf(RunState.PlayerFleetUnitId);
		var destination = map.DocksByPoiId[SupplySystemPlan.Copper.StoragePoiId].Position;

		var result = QueueMove(orchestrator, destination);

		Assert.IsType<MoveCommandResult.Queued>(result);
		Assert.Equal(EPhase.Docked, player.State.Phase);
		Assert.False(player.State.Journey.IsActive);
		Assert.Null(player.Runtime.CachedPath);
		Assert.Equal(destination, orchestrator.PlayerAgent!.PendingMove!.Destination);
	}

	[Fact]
	public void OrderMove_CommitsMoveOnNextTick()
	{
		var orchestrator = CreatePlayerOrchestrator(42);
		var map = orchestrator.Map;
		var player = map.UnitRegistry.UnitOf(RunState.PlayerFleetUnitId);
		var destination = map.DocksByPoiId[SupplySystemPlan.Copper.StoragePoiId].Position;

		QueueMove(orchestrator, destination);
		orchestrator.AdvanceTick();

		Assert.Equal(EPhase.InTransit, player.State.Phase);
		Assert.Equal(destination, player.State.Journey.Destination);
		Assert.NotNull(player.Runtime.CachedPath);
	}

	[Fact]
	public void OrderMove_Unreachable_DoesNotQueue()
	{
		var map = StarMap.CreateDevDefault(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var orchestrator = StarSystemOrchestrator.FromMap(
			map,
			new UnreachablePathfinder(),
			RunState.PlayerFleetUnitId);
		var player = orchestrator.Map.UnitRegistry.UnitOf(RunState.PlayerFleetUnitId);

		var result = QueueMove(orchestrator, new Coord(999, 0, 999));

		Assert.IsType<MoveCommandResult.Unreachable>(result);
		Assert.Equal(EPhase.Docked, player.State.Phase);
		Assert.False(player.State.Journey.IsActive);
		Assert.Null(orchestrator.PlayerAgent!.PendingMove);
	}

	[Fact]
	public void OrderMove_ReroutesWhileInTransit()
	{
		var orchestrator = CreatePlayerOrchestrator(42);
		var map = orchestrator.Map;
		var player = map.UnitRegistry.UnitOf(RunState.PlayerFleetUnitId);
		var firstDestination = map.DocksByPoiId[SupplySystemPlan.Copper.StoragePoiId].Position;
		var secondDestination = map.DocksByPoiId[SupplySystemPlan.Copper.ExitPoiId].Position;

		QueueMove(orchestrator, firstDestination);
		orchestrator.AdvanceTick();
		var firstJourneyId = player.State.Journey.JourneyId;

		QueueMove(orchestrator, secondDestination);
		orchestrator.AdvanceTick();

		Assert.NotEqual(firstJourneyId, player.State.Journey.JourneyId);
		Assert.Equal(secondDestination, player.State.Journey.Destination);
	}

	[Fact]
	public void OrderMove_LatestRequestWithinIntervalWins()
	{
		var orchestrator = CreatePlayerOrchestrator(42);
		var map = orchestrator.Map;
		var firstDestination = map.DocksByPoiId[SupplySystemPlan.Copper.StoragePoiId].Position;
		var secondDestination = map.DocksByPoiId[SupplySystemPlan.Copper.ExitPoiId].Position;

		QueueMove(orchestrator, firstDestination);
		QueueMove(orchestrator, secondDestination);
		orchestrator.AdvanceTick();

		var player = map.UnitRegistry.UnitOf(RunState.PlayerFleetUnitId);
		Assert.Equal(secondDestination, player.State.Journey.Destination);
		Assert.DoesNotContain(
			map.Timeline.History(orchestrator.Tick - 1).OfType<MoveAction>(),
			action => action.UnitId == RunState.PlayerFleetUnitId
				&& action.Destination == firstDestination);
	}

	[Fact]
	public void OrderMove_FreeSpaceArrival_LeavesPlayerIdleAtCoord()
	{
		var orchestrator = CreatePlayerOrchestrator(42);
		var map = orchestrator.Map;
		var player = map.UnitRegistry.UnitOf(RunState.PlayerFleetUnitId);
		var origin = map.DocksByPoiId[SupplySystemPlan.Copper.TradeHubPoiId].Position;
		var destination = new Coord(origin.X + 40, 0, origin.Z + 40);

		QueueMove(orchestrator, destination);
		orchestrator.AdvanceTick();
		var path = player.Runtime.CachedPath!;
		var duration = path.DurationTicks(player.State.SpeedPerTick);
		orchestrator.AdvanceTicks(duration);

		Assert.Equal(EPhase.Docked, player.State.Phase);
		Assert.Equal("", player.State.DockedAtDockId);
		Assert.Equal(destination, player.State.IdleCoord);
	}

	[Fact]
	public void AdvanceTick_IgnoresPlayerFleetInTrafficAgents()
	{
		var orchestrator = CreatePlayerOrchestrator(42);
		var player = orchestrator.Map.UnitRegistry.UnitOf(RunState.PlayerFleetUnitId);

		orchestrator.AdvanceTick();

		Assert.Equal(EPhase.Docked, player.State.Phase);
		Assert.False(player.State.Journey.IsActive);
	}

	[Fact]
	public void OrderMove_UsesPlayerIdAsActorAndUnit()
	{
		var orchestrator = CreatePlayerOrchestrator(42);
		var destination = orchestrator.Map.DocksByPoiId[SupplySystemPlan.Copper.RefineryPoiId].Position;

		QueueMove(orchestrator, destination);
		orchestrator.AdvanceTick();

		var move = orchestrator.Map.Timeline.History(orchestrator.Tick - 1)
			.OfType<MoveAction>()
			.Last(action => action.UnitId == RunState.PlayerFleetUnitId);
		Assert.Equal(RunState.PlayerFleetUnitId, move.ActorId);
		Assert.Equal(RunState.PlayerFleetUnitId, move.UnitId);
	}

	[Fact]
	public void OrderMove_DockArrival_DoesNotScheduleWork()
	{
		var orchestrator = CreatePlayerOrchestrator(42);
		var map = orchestrator.Map;
		var player = map.UnitRegistry.UnitOf(RunState.PlayerFleetUnitId);
		var destination = map.DocksByPoiId[SupplySystemPlan.Copper.StoragePoiId].Position;

		QueueMove(orchestrator, destination);
		orchestrator.AdvanceTick();
		var duration = player.Runtime.CachedPath!.DurationTicks(player.State.SpeedPerTick);
		orchestrator.AdvanceTicks(duration);

		Assert.Equal(EPhase.Docked, player.State.Phase);
		Assert.DoesNotContain(
			map.Timeline.History().OfType<BeginWorkAction>(),
			action => action.UnitId == RunState.PlayerFleetUnitId);
	}

	[Fact]
	public void RunAssembly_SpawnsPlayerFleetAtTradeHub()
	{
		var orchestrator = CreatePlayerOrchestrator(42);
		var map = orchestrator.Map;

		Assert.Equal(RunState.PlayerFleetUnitId, orchestrator.PlayerId);
		var player = map.UnitRegistry.UnitOf(RunState.PlayerFleetUnitId);
		Assert.Equal(EType.PlayerFleet, player.State.Type);
		Assert.Empty(player.State.ChoreDockIds);
		Assert.NotNull(player.Runtime);
	}

	[Fact]
	public void RegenerateAssembly_RespawnsPlayerFleet()
	{
		var first = CreatePlayerOrchestrator(7);
		var second = CreatePlayerOrchestrator(7);

		Assert.Equal(first.PlayerId, second.PlayerId);
		Assert.True(second.Map.UnitRegistry.Contains(RunState.PlayerFleetUnitId));
		Assert.Equal(EPhase.Docked, second.Map.UnitRegistry.UnitOf(RunState.PlayerFleetUnitId).State.Phase);
	}

	[Fact]
	public void State_Clone_DoesNotCopyRuntime()
	{
		var orchestrator = CreatePlayerOrchestrator(42);
		var player = orchestrator.Map.UnitRegistry.UnitOf(RunState.PlayerFleetUnitId);
		player.Runtime.JourneyIdSequence = 17;

		var clonedState = player.State.Clone();

		Assert.NotSame(player.State, clonedState);
		Assert.Equal(player.State.Id, clonedState.Id);
		Assert.Equal(17, player.Runtime.JourneyIdSequence);
	}

	[Fact]
	public void Fork_ClonesUnitRuntimeIndependently()
	{
		var orchestrator = CreatePlayerOrchestrator(42);
		var player = orchestrator.Map.UnitRegistry.UnitOf(RunState.PlayerFleetUnitId);
		QueueMove(
			orchestrator,
			orchestrator.Map.DocksByPoiId[SupplySystemPlan.Copper.StoragePoiId].Position);
		orchestrator.AdvanceTick();
		player.Runtime.JourneyIdSequence = 42;

		var forkedPlayer = orchestrator.Map.Fork().UnitRegistry.UnitOf(RunState.PlayerFleetUnitId);

		Assert.NotSame(player.Runtime, forkedPlayer.Runtime);
		player.Runtime.JourneyIdSequence = 99;
		Assert.Equal(42, forkedPlayer.Runtime.JourneyIdSequence);
	}

	[Fact]
	public void OrderMove_Unreachable_PreservesActiveJourney()
	{
		var map = StarMap.CreateDevDefault(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var orchestrator = StarSystemOrchestrator.FromMap(
			map,
			new FirstFoundThenUnreachablePathfinder(),
			RunState.PlayerFleetUnitId);
		var player = orchestrator.Map.UnitRegistry.UnitOf(RunState.PlayerFleetUnitId);
		var destination = orchestrator.Map.DocksByPoiId[SupplySystemPlan.Copper.StoragePoiId].Position;

		QueueMove(orchestrator, destination);
		orchestrator.AdvanceTick();
		var journeyId = player.State.Journey.JourneyId;
		var cachedPath = player.Runtime.CachedPath;

		var result = QueueMove(orchestrator, new Coord(999, 0, 999));

		Assert.IsType<MoveCommandResult.Unreachable>(result);
		Assert.Equal(journeyId, player.State.Journey.JourneyId);
		Assert.Same(cachedPath, player.Runtime.CachedPath);
		Assert.Equal(EPhase.InTransit, player.State.Phase);
	}

	[Fact]
	public void OrderMove_CompletesOnScheduledTick()
	{
		var orchestrator = CreatePlayerOrchestrator(42);
		var player = orchestrator.Map.UnitRegistry.UnitOf(RunState.PlayerFleetUnitId);
		var destination = orchestrator.Map.DocksByPoiId[SupplySystemPlan.Copper.StoragePoiId].Position;

		QueueMove(orchestrator, destination);
		orchestrator.AdvanceTick();
		var duration = player.Runtime.CachedPath!.DurationTicks(player.State.SpeedPerTick);
		var completionTick = player.State.Journey.StartTick + duration;

		while (orchestrator.Tick < completionTick)
		{
			Assert.Equal(EPhase.InTransit, player.State.Phase);
			orchestrator.AdvanceTick();
		}

		Assert.Equal(EPhase.Docked, player.State.Phase);
		Assert.Equal(destination, orchestrator.Map.DocksById[player.State.DockedAtDockId].Position);
	}

	[Fact]
	public void JourneyState_StoresMetadataOnly()
	{
		var propertyNames = typeof(JourneyState)
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Select(property => property.Name)
			.OrderBy(name => name);

		Assert.Equal(
			["Destination", "IsActive", "JourneyId", "Origin", "StartTick"],
			propertyNames);
	}

	private static MoveCommandResult QueueMove(StarSystemOrchestrator orchestrator, Coord destination) =>
		orchestrator.PlayerAgent!.TryQueueMove(destination);

	private static StarSystemOrchestrator CreatePlayerOrchestrator(int seed) =>
		StarSystemTestHarness.CreatePlayerOrchestrator(RunState.PlayerFleetUnitId, seed);

	private sealed class UnreachablePathfinder : IPathfinder
	{
		public PathfindingResult FindPath(Coord origin, Coord destination) =>
			new PathfindingResult.Unreachable();
	}

	private sealed class FirstFoundThenUnreachablePathfinder : IPathfinder
	{
		private int _calls;

		public PathfindingResult FindPath(Coord origin, Coord destination)
		{
			if (_calls++ == 0)
			{
				return new PathfindingResult.Found(
					TransitPath.FromPoints([origin, destination], [1.0, 1.0]));
			}

			return new PathfindingResult.Unreachable();
		}
	}
}
