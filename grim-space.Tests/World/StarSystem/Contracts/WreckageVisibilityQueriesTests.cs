using GrimSpace.Core.Engine;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using RunState = GrimSpace.Run.State;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class WreckageVisibilityQueriesTests(StarMapFixture maps)
{
	[Fact]
	public void VisibleForHolder_IncludesActiveUninvestigatedWreckageOnly()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var origin = map.StateOf(RunState.PlayerFleetUnitId).PositionAt(map, null, 0f).Position;
		var contract = RegisterWreckage(map, "wreck-visible", origin);
		var engine = CreateEngine(map);
		ActorRuntime RuntimeFor(string id) => engine.ActorRuntimes.For(id);

		Assert.Empty(WreckageVisibilityQueries.VisibleForHolder(map, RunState.PlayerFleetUnitId, RuntimeFor, 0f));

		engine.Commit(ContractActionTestContext.Accept(map, RunState.PlayerFleetUnitId, contract.Id));
		Assert.Single(
			WreckageVisibilityQueries.VisibleForHolder(map, RunState.PlayerFleetUnitId, RuntimeFor, 0f),
			site => site.ContractId == contract.Id);

		var wreckage = (WreckageObjective)contract.Objective;
		foreach (var record in new RecordWreckInvestigatedEffect(contract.Id, wreckage.WreckageId)
			.Apply(map, engine.ActorRuntimes.For(RunState.PlayerFleetUnitId), RunState.PlayerFleetUnitId))
			map.Timeline.Append(record);
		Assert.Empty(WreckageVisibilityQueries.VisibleForHolder(map, RunState.PlayerFleetUnitId, RuntimeFor, 0f));
	}

	[Fact]
	public void TryPickAt_SelectsNearestWreckWithinRadius()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var origin = map.StateOf(RunState.PlayerFleetUnitId).PositionAt(map, null, 0f).Position;
		var near = RegisterWreckage(map, "wreck-near", new Coord(origin.X + 1, 0, origin.Z + 1));
		var far = RegisterWreckage(map, "wreck-far", new Coord(origin.X + 2, 0, origin.Z + 1));
		var engine = CreateEngine(map);
		engine.Commit(ContractActionTestContext.Accept(map, RunState.PlayerFleetUnitId, near.Id));
		engine.Commit(ContractActionTestContext.Accept(map, RunState.PlayerFleetUnitId, far.Id));

		Assert.True(WreckageVisibilityQueries.TryPickAt(
			map,
			RunState.PlayerFleetUnitId,
			new Coord(origin.X + 1, 0, origin.Z + 1),
			engine.ActorRuntimes.For,
			0f,
			out var picked));
		Assert.Equal(near.Id, picked.ContractId);
	}

	[Fact]
	public void WreckOutsideVision_IsNeitherVisibleNorPickable()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var origin = map.StateOf(RunState.PlayerFleetUnitId).PositionAt(map, null, 0f).Position;
		var nearPosition = new Coord(origin.X + 120, 0, origin.Z);
		var farPosition = new Coord(origin.X, 0, origin.Z + 121);
		var near = RegisterWreckage(map, "wreck-in-range", nearPosition);
		var far = RegisterWreckage(map, "wreck-out-of-range", farPosition);
		var engine = CreateEngine(map);
		engine.Commit(ContractActionTestContext.Accept(map, RunState.PlayerFleetUnitId, near.Id));
		engine.Commit(ContractActionTestContext.Accept(map, RunState.PlayerFleetUnitId, far.Id));

		Assert.Single(
			WreckageVisibilityQueries.VisibleForHolder(
				map, RunState.PlayerFleetUnitId, engine.ActorRuntimes.For, 0f),
			site => site.ContractId == near.Id);
		Assert.False(WreckageVisibilityQueries.TryPickAt(
			map, RunState.PlayerFleetUnitId, farPosition, engine.ActorRuntimes.For, 0f, out _));
		Assert.True(WreckageVisibilityQueries.TryPickAt(
			map, RunState.PlayerFleetUnitId, nearPosition, engine.ActorRuntimes.For, 0f, out _));
	}

	[Fact]
	public void VisibleForHolder_InTransit_UsesFractionalFleetPosition()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var origin = map.StateOf(RunState.PlayerFleetUnitId).PositionAt(map, null, 0f).Position;
		var contract = RegisterWreckage(
			map, "wreck-entering-range", new Coord(origin.X + 127, 0, origin.Z));
		var engine = CreateEngine(map);
		engine.Commit(ContractActionTestContext.Accept(map, RunState.PlayerFleetUnitId, contract.Id));

		using var orchestrator = StarSystemTestHarness.CreatePlayerOrchestrator(
			maps, RunState.PlayerFleetUnitId, map: map);
		orchestrator.PlayerAgent!.TryQueueMove(new Coord(origin.X + 20, 0, origin.Z));
		orchestrator.AdvanceTick();

		Assert.Empty(WreckageVisibilityQueries.VisibleForHolder(
			map, RunState.PlayerFleetUnitId, orchestrator.RuntimeFor, 0f));
		Assert.Single(WreckageVisibilityQueries.VisibleForHolder(
			map, RunState.PlayerFleetUnitId, orchestrator.RuntimeFor, 0.5f));
	}

	private static Engine<StarMap, ActorRuntime> CreateEngine(StarMap map)
	{
		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(RunState.PlayerFleetUnitId);
		return new Engine<StarMap, ActorRuntime>(map, runtimes);
	}

	private static Contract RegisterWreckage(StarMap map, string contractId, Coord? position = null)
	{
		var center = position ?? new Coord(map.Width / 2, 0, map.Height / 2);
		var searchArea = new AreaPick(
			new AreaIntel(
				"Somewhere near {A}.",
				ContractActionTestContext.AdministrativePoiId,
				ContractActionTestContext.AdministrativePoiId,
				ContractActionTestContext.AdministrativePoiId),
			[center]);
		var contract = new Contract(
			contractId,
			new WreckageObjective($"{contractId}.wreckage", searchArea, new WreckageOutcome.Salvage(ResourceBundle.Empty)),
			GrimSpace.World.StarSystem.Encounter.EDangerLevel.VeryLow,
			map.ControllingFaction,
			ContractActionTestContext.AdministrativePoiId,
			new ContractTerms(ResourceBundle.Of(ResourceId.Credits, 50)),
			new ContractNarrative("Wreck", "Briefing."));
		Assert.True(map.ContractRegistry.TryAdd(contract));
		return contract;
	}
}
