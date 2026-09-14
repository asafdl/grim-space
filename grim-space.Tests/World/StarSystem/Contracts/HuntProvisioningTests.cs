using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Traffic;
using BattleUnitType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

public sealed class HuntProvisioningTests(StarMapFixture maps)
{
	[Fact]
	public void OfferedContractsSpawnNothing()
	{
		var map = maps.Fresh(42);
		var initialCount = map.FleetRegistry.All.Count();

		Assert.Single(map.ContractRegistry.Offered);
		Assert.Equal(initialCount, map.FleetRegistry.All.Count());
	}

	[Fact]
	public void AcceptSingleHuntSpawnsPirateFleet()
	{
		var (engine, unitId, contractId) = CreateEngineAtIssuerDock(42);
		engine.Commit(new AcceptContractAction(unitId, contractId));

		var spawned = engine.World.FleetRegistry.All
			.Single(unit => unit.State.Type == EType.PirateFleet);

		Assert.Equal(EFaction.Pirates, spawned.State.Faction);
		Assert.Equal(EDangerLevel.VeryLow, spawned.State.CombatProfile?.Danger);
		Assert.Equal(EPhase.Docked, spawned.State.Phase);
		Assert.True(spawned.State.IdleCoord != default);
		Assert.Empty(spawned.State.DockedAtDockId);
		Assert.Equal(3, spawned.Members.Count);
		Assert.All(spawned.Members, member =>
		{
			Assert.Equal(GrimSpace.Units.Enums.EType.Patrol, member.Type);
			Assert.StartsWith("patrol-", member.Id);
		});
		Assert.Equal(
			spawned.Members.Count,
			spawned.Members.Select(member => member.Id).Distinct(StringComparer.Ordinal).Count());
	}

	[Fact]
	public void PreviewReevaluationAndCommitPreserveFleetMemberIds()
	{
		var (engine, unitId, contractId) = CreateEngineAtIssuerDock(42);
		var action = new AcceptContractAction(unitId, contractId);
		var sim = engine.CreateSimulation();

		Assert.True(sim.TryEnqueue(action));
		var previewIds = PirateMemberIds(sim.World);

		sim.Reevaluate();
		Assert.Equal(previewIds, PirateMemberIds(sim.World));

		engine.Commit(action);
		Assert.Equal(previewIds, PirateMemberIds(engine.World));
	}

	[Fact]
	public void AcceptHunt_UsesCompositionDeclaredBySpawnSpec()
	{
		var map = maps.Fresh(42);
		var searchArea = CreateSyntheticSearchArea(map);
		var spawnSpec = new FleetSpawnSpec(
			EType.PirateFleet,
			EFaction.Pirates,
			EDangerLevel.VeryLow,
			7,
			[BattleUnitType.Carrier, BattleUnitType.Fighter]);
		var objective = new HuntObjective(
			[new SpawnEncounterGroup("mixed-fleet", searchArea, 1, spawnSpec)]);
		RegisterSyntheticContract(map, "mixed-hunt", objective);

		var unitId = map.FleetRegistry.Ids.First();
		DockAtIssuer(map, unitId);
		var engine = CreateEngine(map, unitId);
		engine.Commit(new AcceptContractAction(unitId, "mixed-hunt"));

		var fleet = engine.World.FleetRegistry.All
			.Single(unit => unit.State.Id.Contains("mixed-fleet", StringComparison.Ordinal));
		Assert.Equal(
			[BattleUnitType.Carrier, BattleUnitType.Fighter],
			fleet.Members.Select(member => member.Type));
	}

	[Fact]
	public void AcceptMultiGroupMultiCount_ProvisionsExpectedFleetTotal()
	{
		var map = maps.Fresh(42);
		var searchArea = CreateSyntheticSearchArea(map);
		var objective = new HuntObjective(
		[
			new SpawnEncounterGroup("alpha", searchArea, 2, CreateSpawnSpec(map.Seed, "alpha")),
			new SpawnEncounterGroup("beta", searchArea, 1, CreateSpawnSpec(map.Seed, "beta")),
		]);
		RegisterSyntheticContract(map, "multi-hunt", objective);

		var unitId = map.FleetRegistry.Ids.First();
		DockAtIssuer(map, unitId);
		var engine = CreateEngine(map, unitId);
		engine.Commit(new AcceptContractAction(unitId, "multi-hunt"));

		var pirateFleets = engine.World.FleetRegistry.All
			.Where(unit => unit.State.Type == EType.PirateFleet)
			.ToList();
		Assert.Equal(3, pirateFleets.Count);
		Assert.Equal(2, pirateFleets.Count(unit => unit.State.Id.Contains("alpha", StringComparison.Ordinal)));
		Assert.Single(pirateFleets, unit => unit.State.Id.Contains("beta", StringComparison.Ordinal));
	}

	[Fact]
	public void ProvisioningDeterministic_PlanIsStableForSameInputs()
	{
		var map = maps.Fresh(42);
		var contract = map.ContractRegistry.Offered.First();
		var hunt = (HuntObjective)contract.Objective;

		var first = ContractFleetPlacement.Plan(
			hunt.SpawnGroups,
			contract.Id,
			map.Seed,
			map,
			map.FleetRegistry);
		var second = ContractFleetPlacement.Plan(
			hunt.SpawnGroups,
			contract.Id,
			map.Seed,
			map,
			map.FleetRegistry);

		Assert.Equal(
			first.Select(spawn => (spawn.UnitId, spawn.Coord.X, spawn.Coord.Z)).ToList(),
			second.Select(spawn => (spawn.UnitId, spawn.Coord.X, spawn.Coord.Z)).ToList());
	}

	[Fact]
	public void ProvisioningDeterministic_ForkPreservesPlannedProvisioning()
	{
		var (engine, unitId, contractId) = CreateEngineAtIssuerDock(42);
		engine.Commit(new AcceptContractAction(unitId, contractId));
		var fork = engine.World.Fork();

		var original = CaptureProvisioning(engine.World);
		var forked = CaptureProvisioning(fork);
		Assert.Equal(original, forked);
	}

	[Fact]
	public void SpawnBindingsMatchGroups()
	{
		var (engine, unitId, contractId) = CreateEngineAtIssuerDock(42);
		engine.Commit(new AcceptContractAction(unitId, contractId));

		var contract = engine.World.ContractRegistry.All.First(c => c.Id == contractId);
		var hunt = (HuntObjective)contract.Objective;
		Assert.True(engine.World.ContractRegistry.TryGetState(contractId, out var state));

		Assert.Equal(hunt.SpawnGroups.Count, state.SpawnBindings.Count);
		foreach (var group in hunt.SpawnGroups)
		{
			Assert.True(state.SpawnBindings.TryGetValue(group.GroupId, out var unitIds));
			Assert.Equal(group.RequiredCount, unitIds.Count);
		}
	}

	[Fact]
	public void DuplicateUnitIdFailsBeforeMutation()
	{
		var map = maps.Fresh(42);
		var unitId = map.FleetRegistry.Ids.First();
		DockAtIssuer(map, unitId);

		var contract = map.ContractRegistry.Offered.First();
		var groupId = ((HuntObjective)contract.Objective).SpawnGroups[0].GroupId;
		var existingId = $"{contract.Id}.{groupId}.0";
		map.FleetRegistry.Add(StarSystemTestHarness.CreatePirateFleet(
			existingId,
			new Coord(10, 0, 10),
			EFaction.Pirates,
			new CombatProfile(EDangerLevel.VeryLow, 1)));

		var engine = CreateEngine(map, unitId);
		Assert.Throws<InvalidOperationException>(() =>
			engine.Commit(new AcceptContractAction(unitId, contract.Id)));
		Assert.True(map.ContractRegistry.IsOffered(contract.Id));
		Assert.Equal(1, map.FleetRegistry.All.Count(unit => unit.State.Type == EType.PirateFleet));
	}

	[Fact]
	public void PreviewDequeueUndoesSpawnsAndActivation()
	{
		var (engine, unitId, contractId) = CreateEngineAtIssuerDock(42);
		var initialCount = engine.World.FleetRegistry.All.Count();
		var sim = engine.CreateSimulation();

		Assert.True(sim.TryEnqueue(new AcceptContractAction(unitId, contractId)));
		Assert.Equal(initialCount + 1, sim.World.FleetRegistry.All.Count());
		Assert.False(sim.World.ContractRegistry.IsOffered(contractId));

		sim.Dequeue();

		Assert.Equal(initialCount, sim.World.FleetRegistry.All.Count());
		Assert.True(sim.World.ContractRegistry.IsOffered(contractId));
		Assert.False(sim.World.ContractRegistry.TryGetState(contractId, out _));
	}

	private static IReadOnlyList<(string UnitId, int X, int Z)> CaptureProvisioning(StarMap map) =>
		map.FleetRegistry.All
			.Where(unit => unit.State.Type == EType.PirateFleet)
			.OrderBy(unit => unit.State.Id, StringComparer.Ordinal)
			.Select(unit => (unit.State.Id, unit.State.IdleCoord.X, unit.State.IdleCoord.Z))
			.ToList();

	private static string[] PirateMemberIds(StarMap map) =>
		map.FleetRegistry.All
			.Where(fleet => fleet.State.Type == EType.PirateFleet)
			.SelectMany(fleet => fleet.Members)
			.Select(member => member.Id)
			.OrderBy(id => id, StringComparer.Ordinal)
			.ToArray();

	private static void RegisterSyntheticContract(StarMap map, string contractId, HuntObjective objective)
	{
		var plan = map.Blueprint.SupplyPlan;
		var searchArea = objective.SpawnGroups[0].SearchArea;
		var contract = new Contract(
			contractId,
			objective,
			map.ControllingFaction,
			plan.AdministrativePoiId,
			new ContractTerms(ResourceBundle.Of(ResourceId.Credits, StarMap.StarterContractRewardCredits)),
			ContractNarrative.ForHunt("Synthetic Hunt", searchArea));
		map.ContractRegistry.RegisterOffered(contract);
	}

	private static AreaPick CreateSyntheticSearchArea(StarMap map)
	{
		var center = new Coord(map.Width / 2, 0, map.Height / 2);
		return new AreaPick(center, 48, "synthetic search sector", default!);
	}

	private static FleetSpawnSpec CreateSpawnSpec(int mapSeed, string groupId) =>
		new(
			EType.PirateFleet,
			EFaction.Pirates,
			EDangerLevel.VeryLow,
			unchecked((int)GrimSpace.Math.StableSeedMixer.From(mapSeed).Add(groupId).Value),
			[BattleUnitType.Patrol]);

	private static void DockAtIssuer(StarMap map, string unitId)
	{
		var issuerDockId = map.DocksByPoiId[map.Blueprint.SupplyPlan.AdministrativePoiId].Id;
		var state = map.FleetRegistry.FleetOf(unitId).State;
		state.Phase = EPhase.Docked;
		state.DockedAtDockId = issuerDockId;
	}

	private static Engine<StarMap, ActorRuntime> CreateEngine(StarMap map, string unitId)
	{
		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(unitId);
		return new Engine<StarMap, ActorRuntime>(map, runtimes);
	}

	private (Engine<StarMap, ActorRuntime> engine, string unitId, string contractId) CreateEngineAtIssuerDock(
		int seed = 42)
	{
		var map = maps.Fresh(seed);
		var unitId = map.FleetRegistry.Ids.First();
		DockAtIssuer(map, unitId);
		var engine = CreateEngine(map, unitId);
		var contractId = map.ContractRegistry.Offered.First().Id;
		return (engine, unitId, contractId);
	}
}
