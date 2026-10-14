using Godot;
using GrimSpace.Components;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Presentation.Scene;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed class MerchantPresenter
{
	private readonly FacilitySceneBinding _binding;
	private readonly DockyardHudOverlay _upgradeHud;
	private readonly ShipRecruitmentHudOverlay _recruitmentHud;
	private readonly Action _onStateChanged;
	private EMerchantSubtype? _activeSubtype;
	private MerchantEndpoint _endpoint = null!;

	public MerchantPresenter(
		Control host,
		FacilitySceneBinding binding,
		Action onStateChanged)
	{
		ArgumentNullException.ThrowIfNull(host);
		ArgumentNullException.ThrowIfNull(binding);
		ArgumentNullException.ThrowIfNull(onStateChanged);

		_binding = binding;
		_onStateChanged = onStateChanged;

		var layer = new CanvasLayer { Layer = 5 };
		host.AddChild(layer);
		_upgradeHud = new DockyardHudOverlay();
		_upgradeHud.PurchaseRequested += OnPurchaseRequested;
		_upgradeHud.Closed += _onStateChanged;
		layer.AddChild(_upgradeHud);
		_recruitmentHud = new ShipRecruitmentHudOverlay();
		_recruitmentHud.RecruitmentRequested += OnRecruitmentRequested;
		_recruitmentHud.Closed += _onStateChanged;
		layer.AddChild(_recruitmentHud);
		_binding.WorldUpdated += Sync;
		_binding.MerchantCatalogQuoted += _upgradeHud.ReceiveCatalogQuote;
	}

	public bool IsOpen => _upgradeHud.IsOpen || _recruitmentHud.IsOpen;

	public void Open(
		FacilityOperator facilityOperator,
		OperatorInteraction interaction)
	{
		if (interaction.Role != EFacilityOperatorRole.Merchant
			|| interaction.MerchantSubtype is not { } subtype)
		{
			throw new ArgumentException(
				$"Operator '{facilityOperator.Name}' does not have a merchant interaction.",
				nameof(interaction));
		}

		_activeSubtype = subtype;
		_endpoint = MerchantEndpointResolver.ForFacilityOperator(
			_binding.Map,
			_binding.PoiId,
			_binding.FacilityId,
			facilityOperator.Name);
		var title = OperatorDisplayLabels.Title(facilityOperator, interaction);
		switch (MerchantSubtypeRouting.PresentationFor(subtype))
		{
			case EMerchantPresentation.Upgrades:
				_upgradeHud.Open(
					_binding.Run,
					_binding.Map,
					title,
					subtype,
					_binding.Intents,
					_endpoint,
					MerchantSubtypeRouting.CatalogFor(subtype));
				break;
			case EMerchantPresentation.Recruitment:
				_recruitmentHud.Open(_binding.Run, title);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(subtype), subtype, null);
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
		var subtype = _activeSubtype
			?? throw new InvalidOperationException(
				"Merchant purchases require an active merchant subtype.");
		var before = _binding.Run.ItemInventory.Get(shipId).Clone();
		if (!_binding.Intents.TryPurchase(
			    _endpoint,
			    MerchantSubtypeRouting.CatalogFor(subtype),
			    offer.Offering,
			    target,
			    before,
			    offer.Cost))
		{
			if (!_binding.Map.PlayerResources.CanApply(offer.Cost.Negate()))
				_binding.PresentInsufficientFunds();
			else
				_binding.PresentSystemError(
					"Purchase failed.",
					"Merchant purchase was rejected after presenting a valid offer.");
			_upgradeHud.RefreshCatalog();
			_onStateChanged();
			return;
		}

		_upgradeHud.RefreshCatalog();
		_onStateChanged();
	}

	// TODO: we need to rework this when we decouple pilots from ships
	private void OnRecruitmentRequested(RecruitmentOffer offer)
	{
		var committed = _binding.Intents.TryRecruitShip(_endpoint, offer);
		if (!committed)
		{
			var run = _binding.Run;
			if (run.PilotRegistry.Active.Count >= EnlistPlayerShipActionDef.MaxPlayerShips)
				_binding.PresentSystemError(
					"Could not recruit ship.",
					"Ship recruitment was requested while the fleet roster was full.");
			else if (!_binding.Map.PlayerResources.CanApply(offer.Cost.Negate()))
				_binding.PresentInsufficientFunds();
			else
				_binding.PresentSystemError(
					"Could not recruit ship.",
					"Ship recruitment was rejected despite a valid roster and resources.");
			_recruitmentHud.Sync(_binding.Run);
			_onStateChanged();
			return;
		}

		_recruitmentHud.Sync(_binding.Run);
		_onStateChanged();
	}
}
