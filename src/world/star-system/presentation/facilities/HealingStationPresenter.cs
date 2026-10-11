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
	}

	public bool IsOpen => _hud.IsOpen;

	public void Open(FacilityOperator facilityOperator)
	{
		if (facilityOperator.Role != EFacilityOperatorRole.HealingStation
			|| facilityOperator.MerchantCatalog != EMerchantCatalog.Healing)
			throw new ArgumentException(
				$"Operator '{facilityOperator.Name}' is not a healing station.",
				nameof(facilityOperator));

		_hud.Open(
			_binding.Run,
			_binding.Map,
			OperatorDisplayLabels.Title(facilityOperator));
		_onStateChanged();
	}

	private void Sync() => _hud.Sync(_binding.Run, _binding.Map);

	private void OnPurchaseRequested(
		MerchantCatalog.Offering offering,
		MerchantCatalog.Target target,
		string shipId)
	{
		var operatorName = MapNavigationContext.ActiveOperatorName
			?? throw new InvalidOperationException(
				"Healing purchases require an active facility operator.");
		var before = _binding.Run.ShipRegistry.Get(shipId).Clone();
		if (!_binding.Intents.TryPurchase(
			    _binding.PoiId,
			    _binding.FacilityId,
			    operatorName,
			    EMerchantCatalog.Healing,
			    offering,
			    target,
			    before))
		{
			_hud.ShowError("Unable to complete purchase.");
			_onStateChanged();
			return;
		}

		_hud.Sync(_binding.Run, _binding.Map);
		var message = offering.Kind switch
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
