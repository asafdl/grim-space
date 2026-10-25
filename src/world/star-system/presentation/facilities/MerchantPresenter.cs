using Godot;
using GrimSpace.Components;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Presentation.Scene;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed class MerchantPresenter
{
	private readonly FacilitySceneBinding? _facility;
	private readonly MapSceneBinding? _map;
	private readonly CatalogMerchantHudOverlay _catalogHud;
	private readonly ShipRecruitmentHudOverlay _recruitmentHud;
	private readonly PilotHiringHudOverlay _pilotHiringHud;
	private readonly Action _onStateChanged;
	private EMerchantCatalog? _activeCatalog;
	private MerchantEndpoint _endpoint = null!;
	private string? _activeLeadContractId;
	private string? _meetingFleetId;

	private State Run => _facility?.Run ?? _map!.Run;
	private StarMap Map => _facility?.Map ?? _map!.Map;
	private UserIntentTranslator Intents => _facility?.Intents ?? _map!.Intents;

	public MerchantPresenter(Node host, FacilitySceneBinding binding, Action onStateChanged)
		: this(host, onStateChanged)
	{
		_facility = binding;
		Subscribe(binding);
	}

	public MerchantPresenter(Node host, MapSceneBinding binding, Action onStateChanged)
		: this(host, onStateChanged)
	{
		_map = binding;
		Subscribe(binding);
	}

	private MerchantPresenter(Node host, Action onStateChanged)
	{
		ArgumentNullException.ThrowIfNull(host);
		ArgumentNullException.ThrowIfNull(onStateChanged);
		_onStateChanged = onStateChanged;

		var layer = new CanvasLayer { Layer = 5 };
		host.AddChild(layer);
		_catalogHud = new CatalogMerchantHudOverlay();
		_catalogHud.PurchaseRequested += OnCatalogPurchaseRequested;
		_catalogHud.Closed += OnShopClosed;
		layer.AddChild(_catalogHud);
		_recruitmentHud = new ShipRecruitmentHudOverlay();
		_recruitmentHud.RecruitmentRequested += OnRecruitmentRequested;
		_recruitmentHud.Closed += OnShopClosed;
		layer.AddChild(_recruitmentHud);
		_pilotHiringHud = new PilotHiringHudOverlay();
		_pilotHiringHud.HireRequested += OnPilotHireRequested;
		_pilotHiringHud.Closed += OnShopClosed;
		layer.AddChild(_pilotHiringHud);
	}

	public bool IsOpen =>
		_catalogHud.IsOpen || _recruitmentHud.IsOpen || _pilotHiringHud.IsOpen;

	public bool TryHandleInput(InputEvent @event) =>
		_catalogHud.TryHandleInput(@event)
		|| _recruitmentHud.TryHandleInput(@event)
		|| _pilotHiringHud.TryHandleInput(@event);

	public void Open(FacilityOperator facilityOperator, OperatorInteraction interaction)
	{
		if (interaction.Role != EFacilityOperatorRole.Merchant
			|| interaction.MerchantSubtype is not { } subtype)
		{
			throw new ArgumentException(
				$"Operator '{facilityOperator.Name}' does not have a merchant interaction.",
				nameof(interaction));
		}

		_activeLeadContractId = null;
		_meetingFleetId = null;
		_endpoint = MerchantEndpointResolver.ForFacilityOperator(
			Map,
			_facility!.PoiId,
			_facility.FacilityId,
			facilityOperator.Name);
		if (_endpoint is LeadSaleMerchantEndpoint leadEndpoint)
			_activeLeadContractId = leadEndpoint.ContractId;
		OpenSubtype(
			subtype,
			OperatorDisplayLabels.Title(facilityOperator, interaction),
			_endpoint);
		_onStateChanged();
	}

	public void OpenHealing(FacilityOperator facilityOperator)
	{
		if (facilityOperator.Role != EFacilityOperatorRole.HealingStation
			|| facilityOperator.MerchantCatalog != EMerchantCatalog.Healing)
		{
			throw new ArgumentException(
				$"Operator '{facilityOperator.Name}' is not a healing station.",
				nameof(facilityOperator));
		}

		ClearSession();
		_endpoint = MerchantEndpointResolver.ForFacilityOperator(
			Map,
			_facility!.PoiId,
			_facility.FacilityId,
			facilityOperator.Name);
		_activeCatalog = EMerchantCatalog.Healing;
		_catalogHud.Open(
			Run,
			Intents,
			_endpoint,
			EMerchantCatalog.Healing,
			"Healing station",
			OperatorDisplayLabels.Title(facilityOperator));
		_onStateChanged();
	}

	public void OpenLeadSaleMeeting(string meetingFleetId)
	{
		if (_map is null)
			throw new InvalidOperationException("Lead-sale meetings require a map scene binding.");

		if (!TryResolveActiveLead(Map, meetingFleetId, out var contract, out var sale))
			return;

		_activeLeadContractId = contract.Id;
		_meetingFleetId = meetingFleetId;
		_endpoint = new LeadSaleMerchantEndpoint(contract.Id);
		OpenSubtype(
			sale.MerchantSubtype,
			$"Black market - {sale.MerchantSubtype}",
			_endpoint);
		_onStateChanged();
	}

	private void OpenSubtype(
		EMerchantSubtype subtype,
		string title,
		MerchantEndpoint endpoint)
	{
		switch (MerchantSubtypeRouting.PresentationFor(subtype))
		{
			case EMerchantPresentation.Upgrades:
				_activeCatalog = MerchantSubtypeRouting.CatalogFor(subtype);
				_catalogHud.Open(
					Run,
					Intents,
					endpoint,
					_activeCatalog.Value,
					UpgradeTitle(subtype),
					title,
					subtype);
				break;
			case EMerchantPresentation.Recruitment:
				_recruitmentHud.Open(Run, title);
				break;
			case EMerchantPresentation.PilotHiring:
				var contractId = _activeLeadContractId
					?? throw new InvalidOperationException(
						"Pilot hiring requires an active lead-sale contract.");
				_pilotHiringHud.Open(Run, title, contractId);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(subtype), subtype, null);
		}
	}

	private void Subscribe(FacilitySceneBinding binding)
	{
		binding.WorldUpdated += Sync;
		binding.MerchantCatalogQuoted += _catalogHud.ReceiveCatalogQuote;
	}

	private void Subscribe(MapSceneBinding binding)
	{
		binding.WorldUpdated += Sync;
		binding.MerchantCatalogQuoted += _catalogHud.ReceiveCatalogQuote;
	}

	private void Sync()
	{
		_catalogHud.Sync(Run);
		_recruitmentHud.Sync(Run);
		if (_activeLeadContractId is not null)
			_pilotHiringHud.Sync(Run, _activeLeadContractId);
	}

	private void OnCatalogPurchaseRequested(
		MerchantCatalog.Offer offer,
		MerchantCatalog.Target target,
		string shipId)
	{
		var catalog = _activeCatalog
			?? throw new InvalidOperationException("Catalog purchases require an active catalog.");
		var before = Run.ItemInventory.Get(shipId).Clone();
		if (!Intents.TryPurchase(_endpoint, catalog, offer.Offering, target, before, offer.Cost))
		{
			if (!Map.PlayerResources.CanApply(offer.Cost.Negate()))
				PresentInsufficientFunds();
			else
				PresentSystemError(
					"Purchase failed.",
					"Merchant purchase was rejected after presenting a valid offer.");
			_catalogHud.RefreshCatalog();
			_onStateChanged();
			return;
		}

		_catalogHud.RefreshCatalog();
		_onStateChanged();
	}

	private void OnRecruitmentRequested(RecruitmentOffer offer)
	{
		if (!Intents.TryPurchaseShip(_endpoint, offer))
		{
			if (!Map.PlayerResources.CanApply(offer.Cost.Negate()))
				PresentInsufficientFunds();
			else
				PresentSystemError(
					"Could not purchase ship.",
					"Ship purchase was rejected despite sufficient resources.");
			_recruitmentHud.Sync(Run);
			_onStateChanged();
			return;
		}

		_recruitmentHud.Sync(Run);
		_onStateChanged();
	}

	private void OnPilotHireRequested(int slot)
	{
		if (_endpoint is not LeadSaleMerchantEndpoint endpoint)
			throw new InvalidOperationException("Pilot hiring requires an active lead sale.");
		if (!Intents.TryHirePilot(endpoint, slot))
		{
			if (PilotRecruitment.TryGetHireCandidate(Map, endpoint.ContractId, slot, out var candidate)
				&& !Map.PlayerResources.CanApply(PilotRecruitment.Price(Map, candidate).Negate()))
				PresentInsufficientFunds();
			else
				PresentSystemError(
					"Could not hire pilot.",
					"Lead-sale pilot hire was rejected despite sufficient resources.");
			_pilotHiringHud.Sync(Run, endpoint.ContractId);
			_onStateChanged();
			return;
		}

		_pilotHiringHud.Sync(Run, endpoint.ContractId);
		_onStateChanged();
	}

	private void OnShopClosed()
	{
		var meetingFleetId = _meetingFleetId;
		ClearSession();
		if (_map is not null
			&& meetingFleetId is not null
			&& Map.WaitingForPlayerInput
			&& !_map.TryCommitPlayerInput(new LeaveContactAction(
				State.PlayerFleetUnitId,
				meetingFleetId,
				EContactIntent.BlackMarketMeeting)))
		{
			PresentSystemError(
				"Could not leave the meeting.",
				$"Failed to leave black-market meeting '{meetingFleetId}'.");
		}

		_onStateChanged();
	}

	private void ClearSession()
	{
		_activeCatalog = null;
		_activeLeadContractId = null;
		_meetingFleetId = null;
	}

	private void PresentInsufficientFunds()
	{
		if (_facility is not null)
			_facility.PresentInsufficientFunds();
		else
			_map!.PresentInsufficientFunds();
	}

	private void PresentSystemError(string summary, string technicalDetails)
	{
		if (_facility is not null)
			_facility.PresentSystemError(summary, technicalDetails);
		else
			_map!.PresentSystemError(summary, technicalDetails);
	}

	private static string UpgradeTitle(EMerchantSubtype subtype) =>
		subtype switch
		{
			EMerchantSubtype.Weapons => "Ability upgrades",
			EMerchantSubtype.ShipSupport => "Ship upgrades",
			_ => throw new InvalidOperationException($"Unsupported upgrade subtype '{subtype}'."),
		};

	private static bool TryResolveActiveLead(
		StarMap map,
		string meetingFleetId,
		out Contract contract,
		out BlackMarketSaleObjective sale)
	{
		contract = null!;
		sale = null!;
		foreach (var active in map.ContractRegistry.ActiveFor(State.PlayerFleetUnitId))
		{
			if (active.Definition.Category != EContractCategory.Lead
				|| active.Definition.Objective is not BlackMarketSaleObjective leadSale
				|| leadSale.Destination is not BlackMarketSpaceMeetingDestination meeting
				|| !string.Equals(meeting.MeetingId, meetingFleetId, StringComparison.Ordinal)
				|| active.State is not LeadContractState lead
				|| lead.Status != EContractStatus.Active)
				continue;

			contract = active.Definition;
			sale = leadSale;
			return true;
		}

		return false;
	}
}
