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
		var contract = RegisterWreckage(map, "wreck-visible");
		var engine = CreateEngine(map);

		Assert.Empty(WreckageVisibilityQueries.VisibleForHolder(map, RunState.PlayerFleetUnitId));

		engine.Commit(ContractActionTestContext.Accept(map, RunState.PlayerFleetUnitId, contract.Id));
		Assert.Single(
			WreckageVisibilityQueries.VisibleForHolder(map, RunState.PlayerFleetUnitId),
			site => site.ContractId == contract.Id);

		var wreckage = (WreckageObjective)contract.Objective;
		foreach (var record in new RecordWreckInvestigatedEffect(contract.Id, wreckage.WreckageId)
			.Apply(map, engine.ActorRuntimes.For(RunState.PlayerFleetUnitId), RunState.PlayerFleetUnitId))
			map.Timeline.Append(record);
		Assert.Empty(WreckageVisibilityQueries.VisibleForHolder(map, RunState.PlayerFleetUnitId));
	}

	[Fact]
	public void TryPickAt_SelectsNearestWreckWithinRadius()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var near = RegisterWreckage(map, "wreck-near", new Coord(20, 0, 20));
		var far = RegisterWreckage(map, "wreck-far", new Coord(80, 0, 80));
		var engine = CreateEngine(map);
		engine.Commit(ContractActionTestContext.Accept(map, RunState.PlayerFleetUnitId, near.Id));
		engine.Commit(ContractActionTestContext.Accept(map, RunState.PlayerFleetUnitId, far.Id));

		Assert.True(WreckageVisibilityQueries.TryPickAt(
			map,
			RunState.PlayerFleetUnitId,
			new Coord(21, 0, 21),
			out var picked));
		Assert.Equal(near.Id, picked.ContractId);
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
			new ContractNarrative("Wreck", "Briefing."),
			ContractFactory.IsWreckageObjectiveMet);
		Assert.True(map.ContractRegistry.TryAdd(contract));
		return contract;
	}
}
