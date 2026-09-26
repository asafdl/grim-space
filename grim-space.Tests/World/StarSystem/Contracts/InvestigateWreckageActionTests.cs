using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using RunState = GrimSpace.Run.State;
using GrimSpace.Units.Enums;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem.Traffic;
using BattleUnitType = GrimSpace.Units.Enums.EType;
using FleetType = GrimSpace.World.StarSystem.Units.EType;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class InvestigateWreckageActionTests(StarMapFixture maps)
{
	private const int RewardCredits = 75;
	private const int SalvageScrap = 4;

	[Fact]
	public void InvestigateSalvage_GrantsSalvageAndCompletionPaymentSeparately()
	{
		var (engine, unitId, contractId) = CreateEngine(new WreckageOutcome.Salvage(
			ResourceBundle.Of(ResourceId.ScrapAlloy, SalvageScrap)));
		OpenWreckDecision(engine, unitId, contractId);

		engine.Commit(new InvestigateWreckageAction(unitId, contractId));

		Assert.True(ContractFactory.IsWreckageObjectiveMet(contractId, engine.World, unitId));
		Assert.Equal(SalvageScrap, engine.World.PlayerResources.GetBalance(ResourceId.ScrapAlloy));

		ContractActionTestContext.ReevaluateAndComplete(engine, unitId);

		Assert.True(engine.World.ContractRegistry.IsCompleted(contractId));
		Assert.Equal(RewardCredits, engine.World.PlayerResources.GetBalance(ResourceId.Credits));
		Assert.Equal(SalvageScrap, engine.World.PlayerResources.GetBalance(ResourceId.ScrapAlloy));
	}

	[Fact]
	public void InvestigateAmbush_SpawnsFleetAndCommitsEngagement()
	{
		var ambushSpec = new FleetSpawnSpec(FleetType.PirateFleet, EFaction.Pirates, 9, [(BattleUnitType.Patrol, EShipGearTier.T0)]);
		var (engine, unitId, contractId) = CreateEngine(new WreckageOutcome.Ambush(ambushSpec));
		var ambushUnitId = $"{contractId}.wreckage.ambush";
		OpenWreckDecision(engine, unitId, contractId);

		var history = engine.Commit(new InvestigateWreckageAction(unitId, contractId, "ambush-test"));

		Assert.True(engine.World.FleetRegistry.Contains(ambushUnitId));
		Assert.True(EngagementQueries.TryGetCommittedPlayerEngagement(engine.World, unitId, out _));
		Assert.Contains(
			history,
			entry => entry is Record<EngagementCommitted>);
		Assert.True(ContractFactory.IsWreckageObjectiveMet(contractId, engine.World, unitId));
		Assert.Equal(0, engine.World.PlayerResources.GetBalance(ResourceId.ScrapAlloy));
	}

	[Fact]
	public void Investigate_WhenAlreadyInvestigated_IsIllegal()
	{
		var (engine, unitId, contractId) = CreateEngine(new WreckageOutcome.Salvage(ResourceBundle.Empty));
		OpenWreckDecision(engine, unitId, contractId);
		engine.Commit(new InvestigateWreckageAction(unitId, contractId));

		var runtime = engine.ActorRuntimes.For(unitId);
		Assert.False(InvestigateWreckageDef.Instance.IsLegal(
			new InvestigateWreckageAction(unitId, contractId),
			engine.World,
			runtime));
	}

	[Fact]
	public void Investigate_WithoutPendingDecision_IsIllegal()
	{
		var (engine, unitId, contractId) = CreateEngine(new WreckageOutcome.Salvage(ResourceBundle.Empty));
		PlaceHolderAtWreck(engine, unitId, contractId);

		var sim = engine.CreateSimulation();
		Assert.False(sim.TryEnqueue(new InvestigateWreckageAction(unitId, contractId)));
	}

	private (Engine<StarMap, ActorRuntime> Engine, string UnitId, string ContractId) CreateEngine(
		WreckageOutcome outcome,
		int seed = 42)
	{
		var map = maps.Fresh(seed);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var contract = RegisterWreckageContract(map, "contract-wreckage-test", outcome);

		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(RunState.PlayerFleetUnitId);
		var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);
		return (engine, RunState.PlayerFleetUnitId, contract.Id);
	}

	private static void OpenWreckDecision(Engine<StarMap, ActorRuntime> engine, string unitId, string contractId)
	{
		PlaceHolderAtWreck(engine, unitId, contractId);
		engine.Commit([new ReachWreckageAction(unitId, contractId)]);
	}

	private static void PlaceHolderAtWreck(Engine<StarMap, ActorRuntime> engine, string unitId, string contractId)
	{
		engine.Commit(ContractActionTestContext.Accept(engine.World, unitId, contractId));
		var wreck = (WreckageObjective)engine.World.ContractRegistry.All
			.First(contract => contract.Id == contractId)
			.Objective;
		UpdateLocationEffect.ArriveAtCoord(unitId, wreck.Position)
			.Apply(engine.World, engine.ActorRuntimes.For(unitId), unitId);
	}

	private static Contract RegisterWreckageContract(StarMap map, string contractId, WreckageOutcome outcome)
	{
		var center = new Coord(map.Width / 2, 0, map.Height / 2);
		var searchArea = new AreaPick(
			new AreaIntel(
				"Somewhere near {A}.",
				ContractActionTestContext.AdministrativePoiId,
				ContractActionTestContext.AdministrativePoiId,
				ContractActionTestContext.AdministrativePoiId),
			[center]);
		var contract = new Contract(
			contractId,
			new WreckageObjective($"{contractId}.wreckage", searchArea, outcome),
			EDangerLevel.VeryLow,
			map.ControllingFaction,
			ContractActionTestContext.AdministrativePoiId,
			new ContractTerms(ResourceBundle.Of(ResourceId.Credits, RewardCredits)),
			new ContractNarrative("Test Wreck", "Investigate the debris."),
			ContractFactory.IsWreckageObjectiveMet);
		Assert.True(map.ContractRegistry.TryAdd(contract));
		return contract;
	}
}
