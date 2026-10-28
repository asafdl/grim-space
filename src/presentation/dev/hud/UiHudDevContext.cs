using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Generation;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.World.StarSystem.Presentation.Facilities;
using GrimSpace.World.StarSystem.Presentation.Scene;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Presentation.Dev.Hud;

/// <summary>Generated run state for HUD dev preview scenes (F6).</summary>
public sealed class UiHudDevContext
{
	public const int DefaultSeed = 42;

	public State Run { get; }

	public StarSystemOrchestrator StarSystem => Run.StarSystem;

	public StarMap Map => StarSystem.Map;

	public UserIntentTranslator Intents { get; }

	private UiHudDevContext(State run)
	{
		Run = run;
		StarSystem.RefreshPlayerAgent();
		Intents = new UserIntentTranslator(
			State.PlayerFleetUnitId,
			StarSystem.ImmediatePlayerActions
			?? throw new InvalidOperationException("HUD previews require a player execution agent."));
	}

	public static UiHudDevContext Create(int seed = DefaultSeed)
	{
		var run = State.CreateNewRun(seed, tutorialsEnabled: false);
		run.StarSystem.TryGrantDebugResources(ResourceBundle.Create(
			(ResourceId.Credits, 50_000),
			(ResourceId.ScrapAlloy, 50_000),
			(ResourceId.IndustrialCore, 50_000),
			(ResourceId.Fuel, 50_000)));
		for (var index = 0; index < 4; index++)
			run.StarSystem.TryBumpDebugDangerProgression();
		PopulateContractBoard(run.StarSystem);
		return new UiHudDevContext(run);
	}

	public readonly record struct ContractBoardVisit(
		string PoiId,
		string FacilityId,
		string OperatorName,
		int ContractCount);

	public bool TryFindContractBoardVisit(out ContractBoardVisit visit)
	{
		ContractBoardVisit? best = null;
		foreach (var poi in Map.PointsOfInterest)
		{
			foreach (var assignment in poi.OperatorTemporaryRoles.Assignments(EFacilityOperatorRole.Contracts))
			{
				var pending = assignment.SourceIds.Count(sourceId =>
					Map.ContractRegistry.TryGet(sourceId, out _)
					&& Map.ContractRegistry.IsPending(sourceId));
				if (pending == 0)
					continue;

				var candidate = new ContractBoardVisit(
					poi.Id,
					assignment.FacilityId,
					assignment.OperatorName,
					pending);
				if (best is null || candidate.ContractCount > best.Value.ContractCount)
					best = candidate;
			}
		}

		visit = best ?? default;
		return best is not null;
	}

	public string OperatorName(string poiId, string facilitySlug, string sceneSlotId)
	{
		var facilityId = Facility.ScopedId(poiId, facilitySlug);
		var facility = Map.GetPointOfInterest(poiId).GetFacility(facilityId);
		return facility.Operators.Single(operatorEntry =>
			operatorEntry.SceneSlotId == sceneSlotId).Name;
	}

	public FacilityMerchantEndpoint TradeHubEndpoint(
		string facilitySlug,
		string operatorSceneSlotId) =>
		new(
			SupplySystemPlan.Copper.TradeHubPoiId,
			Facility.ScopedId(SupplySystemPlan.Copper.TradeHubPoiId, facilitySlug),
			OperatorName(
				SupplySystemPlan.Copper.TradeHubPoiId,
				facilitySlug,
				operatorSceneSlotId));

	public ShipInstance SelectedShip()
	{
		var runtime = StarSystem.RuntimeFor(State.PlayerFleetUnitId);
		var shipId = PlayerFleetSelection.EnsureSelectedMember(
			Map,
			runtime,
			State.PlayerFleetUnitId,
			Run.GetFieldedShipIds())
			?? throw new InvalidOperationException("HUD preview run has no fielded ship.");
		return Run.ItemInventory.Get(shipId);
	}

	public MerchantCatalogQuote BuildCatalogQuote(
		MerchantEndpoint endpoint,
		EMerchantCatalog catalog)
	{
		var ship = SelectedShip();
		return new MerchantCatalogQuote(
			endpoint,
			catalog,
			ship.Id,
			MerchantCatalog.ListFor(catalog, ship, Map));
	}

	public void RequestCatalog(
		MerchantEndpoint endpoint,
		EMerchantCatalog catalog) =>
		Intents.TryFetchMerchantCatalog(endpoint, catalog, SelectedShip().Clone());

	public readonly record struct PilotHirePreviewSetup(
		string ContractId,
		string PresentationTitle,
		LeadSaleMerchantEndpoint Endpoint);

	/// <summary>Accepted pilot lead in the same shape production uses before opening <see cref="PilotHiringHudOverlay"/>.</summary>
	public PilotHirePreviewSetup PreparePilotHirePreview()
	{
		var contractId = AcceptDevPilotRecruitmentLead();
		return new(
			contractId,
			PilotSalePresentationLabels.Title(Map, contractId),
			new LeadSaleMerchantEndpoint(contractId));
	}

	private string AcceptDevPilotRecruitmentLead()
	{
		// HUD dev context bumps danger for richer merchant tiers; pilot stock only exists at T0/T0.5.
		Map.ResetDebugDangerProgression();

		if (!DevPilotRecruitmentLeadSpawner.TrySpawn(Map, out var failureReason))
			throw new InvalidOperationException(failureReason ?? "Pilot lead spawn failed.");

		var contractId = Map.ContractRegistry.Pending
			.LastOrDefault(contract =>
				contract.Id.StartsWith("dev-pilot-lead", StringComparison.Ordinal)
				&& contract.Objective is BlackMarketSaleObjective sale
				&& sale.MerchantSubtype == EMerchantSubtype.Pilots)?.Id
			?? throw new InvalidOperationException("Pilot lead contract missing after spawn.");

		StarSystem.RefreshPlayerAgent();
		if (!Intents.TryAcceptContract(contractId) && !IsActivePilotLead(contractId))
			throw new InvalidOperationException($"Could not accept pilot lead '{contractId}'.");

		return contractId;
	}

	public Contract? SampleHuntContract() =>
		Map.ContractRegistry.Pending
			.FirstOrDefault(contract => contract.Objective is HuntObjective);

	private bool IsActivePilotLead(string contractId) =>
		Map.ContractRegistry.TryGetState(contractId, out var state)
		&& state is LeadContractState { Status: EContractStatus.Active };

	private static void PopulateContractBoard(StarSystemOrchestrator orchestrator)
	{
		for (var wave = 0; wave < 12; wave++)
			orchestrator.AdvanceTicks(ContractBoardConfig.DefaultCadenceTicks);
	}
}
