using Godot;
using GrimSpace.Components;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Presentation.Scene;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed class HealingStationPresenter
{
	private readonly FacilitySceneBinding _binding;
	private readonly HealingStationHudOverlay _hud;
	private readonly Action _onStateChanged;
	private MerchantEndpoint _endpoint = null!;

	public HealingStationPresenter(
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
		_hud = new HealingStationHudOverlay();
		_hud.HealingPurchaseRequested += OnPurchaseRequested;
		_hud.Closed += _onStateChanged;
		layer.AddChild(_hud);
		_binding.WorldUpdated += Sync;
		_binding.MerchantCatalogQuoted += _hud.ReceiveCatalogQuote;
	}

	public bool IsOpen => _hud.IsOpen;

	public void Open(FacilityOperator facilityOperator)
	{
		if (facilityOperator.Role != EFacilityOperatorRole.HealingStation
			|| facilityOperator.MerchantCatalog != EMerchantCatalog.Healing)
			throw new ArgumentException(
				$"Operator '{facilityOperator.Name}' is not a healing station.",
				nameof(facilityOperator));

		_endpoint = MerchantEndpointResolver.ForFacilityOperator(
			_binding.Map,
			_binding.PoiId,
			_binding.FacilityId,
			facilityOperator.Name);
		_hud.Open(
			_binding.Run,
			_binding.Map,
			OperatorDisplayLabels.Title(facilityOperator),
			_binding.Intents,
			_endpoint);
		_onStateChanged();
	}

	private void Sync() => _hud.Sync(_binding.Run, _binding.Map);

	private void OnPurchaseRequested(
		MerchantCatalog.Offer offer,
		MerchantCatalog.Target target,
		string shipId)
	{
		var before = _binding.Run.ShipRegistry.Get(shipId).Clone();
		if (!_binding.Intents.TryPurchase(
			    _endpoint,
			    EMerchantCatalog.Healing,
			    offer.Offering,
			    target,
			    before,
			    offer.Cost))
		{
			_hud.ShowError("Unable to complete purchase.");
			_onStateChanged();
			return;
		}

		_hud.Sync(_binding.Run, _binding.Map);
		var message = offer.Offering.Kind switch
		{
			MerchantCatalog.Kind.RepairHull => "Hull repaired.",
			MerchantCatalog.Kind.RechargeAllShields or MerchantCatalog.Kind.RechargeShieldFace =>
				"Shields recharged.",
			_ => "Purchase complete.",
		};
		_hud.ShowConfirmation(message, HudStatusKind.Success);
		_onStateChanged();
	}
}
