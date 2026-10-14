using Godot;
using GrimSpace.Components;
using GrimSpace.Core.Ids;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Presentation.Facilities;

namespace GrimSpace.World.StarSystem.Presentation.Scene;

public sealed class LeadSaleMapMerchantPresenter
{
	private readonly MapSceneBinding _binding;
	private readonly DockyardHudOverlay _upgradeHud;
	private readonly ShipRecruitmentHudOverlay _recruitmentHud;
	private readonly Action _onStateChanged;
	private string? _contractId;
	private string? _meetingFleetId;
	private EMerchantSubtype? _activeSubtype;

	public LeadSaleMapMerchantPresenter(
		Node owner,
		MapSceneBinding binding,
		Action onStateChanged)
	{
		ArgumentNullException.ThrowIfNull(owner);
		ArgumentNullException.ThrowIfNull(binding);
		ArgumentNullException.ThrowIfNull(onStateChanged);

		_binding = binding;
		_onStateChanged = onStateChanged;

		var layer = new CanvasLayer { Layer = 5 };
		owner.AddChild(layer);
		_upgradeHud = new DockyardHudOverlay();
		_upgradeHud.PurchaseRequested += OnPurchaseRequested;
		_upgradeHud.Closed += OnHudClosed;
		layer.AddChild(_upgradeHud);
		_recruitmentHud = new ShipRecruitmentHudOverlay();
		_recruitmentHud.RecruitmentRequested += OnRecruitmentRequested;
		_recruitmentHud.Closed += OnHudClosed;
		layer.AddChild(_recruitmentHud);
		_binding.WorldUpdated += Sync;
		_binding.MerchantCatalogQuoted += _upgradeHud.ReceiveCatalogQuote;
	}

	public bool IsOpen => _upgradeHud.IsOpen || _recruitmentHud.IsOpen;

	public bool TryHandleInput(InputEvent @event) =>
		_upgradeHud.TryHandleInput(@event) || _recruitmentHud.TryHandleInput(@event);

	public void OpenMeeting(string meetingFleetId)
	{
		if (!TryResolveActiveLead(meetingFleetId, out var contract, out var sale))
			return;

		_contractId = contract.Id;
		_meetingFleetId = meetingFleetId;
		_activeSubtype = sale.MerchantSubtype;
		var title = $"Black market — {sale.MerchantSubtype}";
		switch (MerchantSubtypeRouting.PresentationFor(sale.MerchantSubtype))
		{
			case EMerchantPresentation.Upgrades:
				_upgradeHud.Open(
					_binding.Run,
					_binding.Map,
					title,
					sale.MerchantSubtype,
					CreateCatalogIntents(),
					new LeadSaleMerchantEndpoint(contract.Id),
					MerchantSubtypeRouting.CatalogFor(sale.MerchantSubtype));
				break;
			case EMerchantPresentation.Recruitment:
				_recruitmentHud.Open(_binding.Run, title);
				break;
			default:
				throw new ArgumentOutOfRangeException(
					nameof(sale.MerchantSubtype),
					sale.MerchantSubtype,
					null);
		}

		_onStateChanged();
	}

	private void Sync()
	{
		_upgradeHud.Sync(_binding.Run, _binding.Map);
		_recruitmentHud.Sync(_binding.Run);
	}

	private void OnPurchaseRequested(
		MerchantCatalog.Offer offer,
		MerchantCatalog.Target target,
		string shipId)
	{
		var contractId = _contractId
			?? throw new InvalidOperationException("Lead sale purchases require an active contract.");
		var subtype = _activeSubtype
			?? throw new InvalidOperationException("Lead sale purchases require an active merchant subtype.");
		var run = _binding.Run;
		var before = run.ItemInventory.Get(shipId).Clone();
		if (!_binding.TryCommitPlayerInput(new PurchaseAction(
			    State.PlayerFleetUnitId,
			    new LeadSaleMerchantEndpoint(contractId),
			    MerchantSubtypeRouting.CatalogFor(subtype),
			    offer.Offering,
			    target,
			    before,
			    offer.Cost)))
		{
			if (!_binding.Map.PlayerResources.CanApply(offer.Cost.Negate()))
				_binding.PresentInsufficientFunds();
			else
				_binding.PresentSystemError(
					"Purchase failed.",
					"Lead-sale purchase was rejected after presenting a valid offer.");
			_upgradeHud.RefreshCatalog();
			_onStateChanged();
			return;
		}

		_upgradeHud.RefreshCatalog();
		_onStateChanged();
	}

	private UserIntentTranslator CreateCatalogIntents() =>
		new(
			State.PlayerFleetUnitId,
			_binding.StarSystem.ImmediatePlayerActions
			?? throw new InvalidOperationException(
				"Lead sale merchants require immediate player actions."));

	private void OnRecruitmentRequested(RecruitmentOffer offer)
	{
		var contractId = _contractId
			?? throw new InvalidOperationException("Lead sale recruitment requires an active contract.");
		if (!_binding.TryCommitPlayerInput(new RecruitShipAction(
			    State.PlayerFleetUnitId,
			    new LeadSaleMerchantEndpoint(contractId),
			    new ShipSpawnDeclaration(
				    TypedIdGenerator.NextId(UnitTypeSlug.For(offer.Chassis)),
				    offer.Chassis,
				    offer.GearTier))))
		{
			var run = _binding.Run;
			if (run.PilotRegistry.Active.Count >= EnlistPlayerShipActionDef.MaxPlayerShips)
				_binding.PresentSystemError(
					"Could not recruit ship.",
					"Lead-sale recruitment was requested while the fleet roster was full.");
			else if (!_binding.Map.PlayerResources.CanApply(offer.Cost.Negate()))
				_binding.PresentInsufficientFunds();
			else
				_binding.PresentSystemError(
					"Could not recruit ship.",
					"Lead-sale recruitment was rejected despite a valid roster and resources.");
			_recruitmentHud.Sync(run);
			_onStateChanged();
			return;
		}

		_recruitmentHud.Sync(_binding.Run);
		_onStateChanged();
	}

	private void OnHudClosed()
	{
		var meetingFleetId = _meetingFleetId;
		_contractId = null;
		_meetingFleetId = null;
		_activeSubtype = null;
		if (meetingFleetId is not null
			&& _binding.Map.WaitingForPlayerInput
			&& !_binding.TryCommitPlayerInput(new LeaveContactAction(
				State.PlayerFleetUnitId,
				meetingFleetId,
				EContactIntent.BlackMarketMeeting)))
			_binding.PresentSystemError(
				"Could not leave the meeting.",
				$"Failed to leave black-market meeting '{meetingFleetId}'.");

		_onStateChanged();
	}

	private bool TryResolveActiveLead(
		string meetingFleetId,
		out Contract contract,
		out BlackMarketSaleObjective sale)
	{
		contract = null!;
		sale = null!;
		foreach (var active in _binding.Map.ContractRegistry.ActiveFor(State.PlayerFleetUnitId))
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
