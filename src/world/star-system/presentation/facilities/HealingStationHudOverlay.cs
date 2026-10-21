using Godot;
using GrimSpace.Components;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Presentation.Scene;
using GrimSpace.Ui.Components;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed partial class HealingStationHudOverlay : Control
{
	private readonly ModalShell _shell;
	private State _run = null!;
	private UserIntentTranslator? _intents;
	private MerchantEndpoint _endpoint;
	private string _facilityTitle = "";
	private MerchantCatalogQuote? _catalogQuote;

	public event Action<MerchantCatalog.Offer, MerchantCatalog.Target, string>? HealingPurchaseRequested;
	public event Action? Closed;

	public HealingStationHudOverlay()
	{
		AnchorsPreset = (int)LayoutPreset.FullRect;
		AnchorRight = 1f;
		AnchorBottom = 1f;
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;
		MouseFilter = MouseFilterEnum.Ignore;

		_shell = new ModalShell(UiContainerVisual.Modal, HudThemeFamily.Informative);
		AddChild(_shell);
		_shell.SetDismissVisible(true);
		_shell.Closed += () => Closed?.Invoke();
	}

	public bool IsOpen => _shell.IsOpen;

	public void Open(
		State run,
		StarMap map,
		string facilityTitle,
		UserIntentTranslator intents,
		MerchantEndpoint endpoint)
	{
		_run = run;
		_intents = intents;
		_endpoint = endpoint;
		_facilityTitle = facilityTitle;
		_catalogQuote = null;
		_shell.Open("Healing station", facilityTitle);
		RequestCatalog();
	}

	public void Close()
	{
		_catalogQuote = null;
		_shell.Close();
	}

	public void Sync(State run, StarMap map)
	{
		_run = run;
		if (IsOpen && _catalogQuote is not null)
			ShowMain();
	}

	public void RefreshCatalog() => RequestCatalog();

	public void ReceiveCatalogQuote(MerchantCatalogQuote quote)
	{
		if (_intents is null
			|| quote.Catalog != EMerchantCatalog.Healing
			|| !quote.Endpoint.Equals(_endpoint)
			|| !TryGetActiveShip(out var ship)
			|| !string.Equals(quote.ShipId, ship.Id, StringComparison.Ordinal))
			return;

		_catalogQuote = quote;
		if (IsOpen)
			ShowMain();
	}

	private void RequestCatalog()
	{
		if (_intents is null || !IsOpen)
			return;

		if (!TryGetActiveShip(out var ship))
		{
			_catalogQuote = null;
			ShowMain();
			return;
		}

		_catalogQuote = null;
		if (!_intents.TryFetchMerchantCatalog(
			    _endpoint,
			    EMerchantCatalog.Healing,
			    ship.Clone()))
			ShowMain();
	}

	private void ShowMain()
	{
		_shell.SetTitle("Healing station");
		_shell.SetSubtitle(_facilityTitle);
		_shell.SetHeader(HudHeaderMode.Close);
		_shell.SetBackHandler(null);
		_shell.SetFooter([]);

		var body = HudWidgets.CreateCardList();
		body.AddThemeConstantOverride("separation", 18);
		AddStatus(body);

		if (!TryGetActiveShip(out var ship))
		{
			body.AddChild(HudWidgets.CreateStatusPanel(HudStatusKind.Neutral, "No ships in your party."));
			_shell.SetBody(body);
			return;
		}

		if (_catalogQuote is null)
		{
			body.AddChild(HudWidgets.CreateStatusPanel(
				HudStatusKind.Neutral,
				"Loading services…"));
			_shell.SetBody(body);
			return;
		}

		var offers = _catalogQuote.Offers;
		foreach (var offer in offers)
		{
			var captured = offer;
			var target = captured.Targets.Single();
			var canAfford = _run.StarSystem.Map.PlayerResources.CanApply(captured.Cost.Negate());
			body.AddChild(MerchantUiListItem.Create(
				ServiceTitle(captured, target),
				ServiceBody(captured, target, ship),
				captured.Cost,
				canAfford,
				() => HealingPurchaseRequested?.Invoke(captured, target, ship.Id)));
		}

		if (offers.Count == 0)
			body.AddChild(HudWidgets.CreateStatusPanel(
				HudStatusKind.Neutral,
				"No support services are currently needed."));

		_shell.SetBody(body);
	}

	private static string ServiceTitle(
		MerchantCatalog.Offer offer,
		MerchantCatalog.Target target) =>
		offer.Offering.Kind switch
		{
			MerchantCatalog.Kind.RepairHull => "Repair hull",
			MerchantCatalog.Kind.RechargeAllShields => "Recharge all shields",
			MerchantCatalog.Kind.RechargeShieldFace when target.Face is { } face =>
				$"Recharge {MerchantOfferDisplay.FaceLabel(face).ToLowerInvariant()} shields",
			_ => throw new InvalidOperationException(
				$"Unsupported ship support service {offer.Offering.Kind}."),
		};

	private static string ServiceBody(
		MerchantCatalog.Offer offer,
		MerchantCatalog.Target target,
		ShipInstance ship) =>
		offer.Offering.Kind switch
		{
			MerchantCatalog.Kind.RepairHull =>
				MerchantOfferDisplay.HullRepairBody(
					ship.HullPoints,
					ship.Loadout.MaxHullPoints),
			MerchantCatalog.Kind.RechargeAllShields =>
				MerchantOfferDisplay.RechargeAllBody(
					ship.TotalCurrentShieldPoints,
					ship.TotalMaxShieldPoints),
			MerchantCatalog.Kind.RechargeShieldFace when target.Face is { } face =>
				MerchantOfferDisplay.ShieldRechargeBody(
					System.Math.Clamp(
						ship.ShieldPoints[face],
						0,
						ship.Loadout.MaxShieldPoints[face]),
					ship.Loadout.MaxShieldPoints[face]),
			_ => throw new InvalidOperationException(
				$"Unsupported ship support service {offer.Offering.Kind}."),
		};

	private void AddStatus(VBoxContainer body)
	{
	}

	private bool TryGetActiveShip(out ShipInstance ship)
	{
		ship = null!;
		var runtime = _run.StarSystem.RuntimeFor(State.PlayerFleetUnitId);
		var shipId = PlayerFleetSelection.EnsureSelectedMember(
			_run.StarSystem.Map,
			runtime,
			State.PlayerFleetUnitId,
			_run.GetFieldedShipIds());
		if (shipId is null)
			return false;

		ship = _run.ItemInventory.Get(shipId);
		return true;
	}
}
