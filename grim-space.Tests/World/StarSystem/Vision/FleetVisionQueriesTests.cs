using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Vision;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Traffic;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.World.StarSystem.Vision;

[StarSystemTestSuite]
public sealed class FleetVisionQueriesTests(StarMapFixture maps)
{
	[Theory]
	[InlineData(7, 7, true)]
	[InlineData(8, 8, false)]
	public void CanSee_DiagonalBoundary_UsesInclusiveObserverRadius(int targetX, int targetZ, bool expected)
	{
		var map = maps.Fresh(42);
		var observerId = AddFleet(map, "observer", new Coord(0, 0, 0), visionRadius: 10);
		var targetId = AddFleet(map, "target", new Coord(targetX, 0, targetZ), visionRadius: 10);

		Assert.Equal(
			expected,
			FleetVisionQueries.CanSee(map, observerId, targetId, _ => new(), 0f));
	}

	[Fact]
	public void CanSee_AsymmetricRadii_UsesObserverRadiusOnly()
	{
		var map = maps.Fresh(42);
		var observerId = AddFleet(map, "observer", new Coord(0, 0, 0), visionRadius: 10);
		var targetId = AddFleet(map, "target", new Coord(20, 0, 0), visionRadius: 100);

		Assert.False(FleetVisionQueries.CanSee(map, observerId, targetId, _ => new(), 0f));
		Assert.True(FleetVisionQueries.CanSee(map, targetId, observerId, _ => new(), 0f));
	}

	[Fact]
	public void CanSee_SameFaction_DoesNotShareVisionWithoutRange()
	{
		var map = maps.Fresh(42);
		var observerId = AddFleet(
			map,
			"observer",
			new Coord(0, 0, 0),
			visionRadius: 10,
			faction: EFaction.Pirates);
		var targetId = AddFleet(
			map,
			"ally",
			new Coord(50, 0, 0),
			visionRadius: 10,
			faction: EFaction.Pirates);

		Assert.False(FleetVisionQueries.CanSee(map, observerId, targetId, _ => new(), 0f));
	}

	[Fact]
	public void CanSee_Self_RequiresBothIdsExist()
	{
		var map = maps.Fresh(42);
		var fleetId = AddFleet(map, "fleet", new Coord(0, 0, 0), visionRadius: 10);

		Assert.True(FleetVisionQueries.CanSee(map, fleetId, fleetId, _ => new(), 0f));
		Assert.False(FleetVisionQueries.CanSee(map, "missing", "missing", _ => new(), 0f));
		Assert.False(FleetVisionQueries.CanSee(map, fleetId, "missing", _ => new(), 0f));
	}

	[Fact]
	public void VisibleTo_MissingObserver_ReturnsEmptySet()
	{
		var map = maps.Fresh(42);
		AddFleet(map, "fleet", new Coord(0, 0, 0), visionRadius: 10);

		var visible = FleetVisionQueries.VisibleTo(map, "missing", _ => new(), 0f);

		Assert.Empty(visible);
	}

	[Fact]
	public void VisibleTo_IncludesSelfAndInRangeTargets()
	{
		var map = maps.Fresh(42);
		var observerId = AddFleet(map, "observer", new Coord(0, 0, 0), visionRadius: 10);
		var nearId = AddFleet(map, "near", new Coord(5, 0, 0), visionRadius: 10);
		var farId = AddFleet(map, "far", new Coord(50, 0, 0), visionRadius: 10);

		var visible = FleetVisionQueries.VisibleTo(map, observerId, _ => new(), 0f);

		Assert.Contains(observerId, visible);
		Assert.Contains(nearId, visible);
		Assert.DoesNotContain(farId, visible);
	}

	[Fact]
	public void CanSee_DockedFleet_UsesDockPosition()
	{
		var map = maps.Fresh(42);
		var dock = map.DocksById.Values.First();
		var observerId = AddFleet(map, "observer", dock.Position, visionRadius: 20);
		var dockedId = AddDockedFleet(map, "docked", dock.Id, visionRadius: 10);

		Assert.True(FleetVisionQueries.CanSee(map, observerId, dockedId, _ => new(), 0f));
	}

	[Fact]
	public void CanSee_InTransit_UsesFractionalCommittedPosition()
	{
		var orchestrator = CreatePlayerOrchestrator(42, visionRadius: 12);
		var map = orchestrator.Map;
		var playerId = RunState.PlayerFleetUnitId;
		var origin = orchestrator.CommittedPositionOf(playerId);
		var destination = new Coord(origin.X + 20, 0, origin.Z);
		orchestrator.PlayerAgent!.TryQueueMove(destination);
		orchestrator.AdvanceTick();

		var targetId = AddFleet(
			map,
			"target",
			new Coord(origin.X + 20, 0, origin.Z),
			visionRadius: 10);

		Assert.False(FleetVisionQueries.CanSee(
			map,
			playerId,
			targetId,
			orchestrator.RuntimeFor,
			0f));
		Assert.True(FleetVisionQueries.CanSee(
			map,
			playerId,
			targetId,
			orchestrator.RuntimeFor,
			0.5f));
	}

	[Fact]
	public void CanSee_PendingPlayerCourse_DoesNotShiftVisionOrigin()
	{
		var orchestrator = CreatePlayerOrchestrator(42, visionRadius: 10);
		var map = orchestrator.Map;
		var playerId = RunState.PlayerFleetUnitId;
		var origin = orchestrator.CommittedPositionOf(playerId);
		var farDestination = new Coord(origin.X + 200, 0, origin.Z + 200);
		var nearTargetId = AddFleet(
			map,
			"near-target",
			new Coord(origin.X + 5, 0, origin.Z),
			visionRadius: 10);

		orchestrator.PlayerAgent!.TryQueueMove(farDestination);

		Assert.True(FleetVisionQueries.CanSee(
			map,
			playerId,
			nearTargetId,
			orchestrator.RuntimeFor,
			0f));
	}

	[Fact]
	public void HiddenFleets_StillSimulateOnTickAdvance()
	{
		var orchestrator = CreatePlayerOrchestrator(42, visionRadius: 1);
		var map = orchestrator.Map;
		var playerId = RunState.PlayerFleetUnitId;
		var pirateId = AddFleet(map, "hidden-pirate", new Coord(100, 0, 100), visionRadius: 10);
		var pirate = map.FleetRegistry.FleetOf(pirateId);
		pirate.State.Phase = EPhase.Docked;
		pirate.State.DockedAtDockId = "";
		pirate.State.IdleCoord = new Coord(100, 0, 100);
		var destination = new Coord(110, 0, 110);
		orchestrator.RuntimeFor(pirateId).CachedPath =
			TransitPath.FromPoints([pirate.State.IdleCoord, destination], [1.0, 1.0]);
		pirate.State.StartJourney(1, pirate.State.IdleCoord, destination, map.Timeline.Clock.Current);

		Assert.False(FleetVisionQueries.CanSee(
			map,
			playerId,
			pirateId,
			orchestrator.RuntimeFor,
			0f));

		orchestrator.AdvanceTick();

		Assert.Equal(EPhase.InTransit, pirate.State.Phase);
		Assert.NotEqual(pirate.State.IdleCoord, orchestrator.CommittedPositionOf(pirateId));
	}

	[Fact]
	public void FromSpawn_CloneAndFork_PreserveVisionRadius()
	{
		var spawn = new Spawn(
			"fleet",
			EType.PirateFleet,
			"",
			new Coord(1, 0, 1),
			UnitDefaults.SpeedPerTick(EType.PirateFleet),
			UnitDefaults.EngageRadius(EType.PirateFleet),
			42,
			[]);
		var state = GrimSpace.World.StarSystem.Units.State.FromSpawn(spawn);
		var clone = state.Clone();
		var map = maps.Fresh(42);
		map.FleetRegistry.Add(Factory.Create(spawn));
		var forked = map.Fork();

		Assert.Equal(42, state.VisionRadius);
		Assert.Equal(42, clone.VisionRadius);
		Assert.Equal(42, forked.FleetRegistry.FleetOf("fleet").State.VisionRadius);
	}

	[Fact]
	public void Factory_RejectsNonPositiveVisionRadius()
	{
		var spawn = new Spawn(
			"fleet",
			EType.PirateFleet,
			"",
			new Coord(1, 0, 1),
			UnitDefaults.SpeedPerTick(EType.PirateFleet),
			UnitDefaults.EngageRadius(EType.PirateFleet),
			0,
			[]);

		Assert.Throws<ArgumentOutOfRangeException>(() => Factory.Create(spawn));
	}

	[Fact]
	public void UnitDefaults_VisionRadius_Is120ForAllTypes()
	{
		foreach (var type in Enum.GetValues<EType>())
			Assert.Equal(120, UnitDefaults.VisionRadius(type));
	}

	private StarSystemOrchestrator CreatePlayerOrchestrator(int seed, double visionRadius = 120)
	{
		var map = maps.Fresh(seed);
		var tradeHubDock = map.DocksByPoiId[SupplySystemPlan.Copper.TradeHubPoiId];
		map.FleetRegistry.Add(Factory.Create(new Spawn(
			RunState.PlayerFleetUnitId,
			EType.PlayerFleet,
			tradeHubDock.Id,
			default,
			UnitDefaults.SpeedPerTick(EType.PlayerFleet),
			UnitDefaults.EngageRadius(EType.PlayerFleet),
			visionRadius,
			[],
			EFaction.Player)));
		return StarSystemTestHarness.CreatePlayerOrchestrator(
			maps,
			RunState.PlayerFleetUnitId,
			seed,
			map: map);
	}

	private static string AddFleet(
		StarMap map,
		string id,
		Coord coord,
		double visionRadius,
		EFaction faction = EFaction.Pirates) =>
		AddDockedFleet(map, id, "", coord, visionRadius, faction);

	private static string AddDockedFleet(
		StarMap map,
		string id,
		string dockId,
		double visionRadius) =>
		AddDockedFleet(map, id, dockId, default, visionRadius);

	private static string AddDockedFleet(
		StarMap map,
		string id,
		string dockId,
		Coord idleCoord,
		double visionRadius,
		EFaction faction = EFaction.Pirates)
	{
		var fleet = Factory.Create(new Spawn(
			id,
			EType.PirateFleet,
			dockId,
			idleCoord,
			UnitDefaults.SpeedPerTick(EType.PirateFleet),
			UnitDefaults.EngageRadius(EType.PirateFleet),
			visionRadius,
			[],
			faction,
			new CombatProfile(EDangerLevel.VeryLow, 1)));
		map.FleetRegistry.Add(fleet);
		return id;
	}
}
