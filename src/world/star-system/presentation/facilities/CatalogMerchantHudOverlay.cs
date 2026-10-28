using Godot;
using GrimSpace.Components;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Presentation.Scene;
using GrimSpace.Ui.Components;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed partial class CatalogMerchantHudOverlay : Control
{
	private readonly ModalShell _shell;
	private State _run = null!;
	private UserIntentTranslator? _intents;
	private MerchantEndpoint _endpoint;
	private EMerchantCatalog _catalog;
	private string _title = "";
	private string _subtitle = "";
	private MerchantCatalogQuote? _catalogQuote;

	public event Action<MerchantCatalog.Offer, MerchantCatalog.Target, string>? PurchaseRequested;
	public event Action? Closed;

	public CatalogMerchantHudOverlay()
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

	public bool TryHandleInput(InputEvent @event) => _shell.TryHandleInput(@event);

	public void Open(
		State run,
		UserIntentTranslator intents,
		MerchantEndpoint endpoint,
		EMerchantCatalog catalog,
		string title,
		string subtitle,
		EMerchantSubtype? upgradeSubtype = null)
	{
		if (catalog is EMerchantCatalog.Weapons or EMerchantCatalog.ShipSupport
			&& upgradeSubtype is not (EMerchantSubtype.Weapons or EMerchantSubtype.ShipSupport))
		{
			throw new ArgumentException(
				"Upgrade merchants require a weapon or ship-support subtype.",
				nameof(upgradeSubtype));
		}

		_run = run;
		_intents = intents;
		_endpoint = endpoint;
		_catalog = catalog;
		_title = title;
		_subtitle = subtitle;
		_catalogQuote = null;
		_shell.Open(title, subtitle);
		RequestCatalog();
	}

	public void Close()
	{
		_catalogQuote = null;
		_shell.Close();
	}

	public void Sync(State run)
	{
		_run = run;
		if (!IsOpen)
			return;

		if (!TryGetActiveShip(out var ship))
		{
			if (_catalogQuote is not null)
			{
				_catalogQuote = null;
				ShowMain();
			}

			return;
		}

		if (_catalogQuote is null
			|| !string.Equals(_catalogQuote.ShipId, ship.Id, StringComparison.Ordinal))
			RequestCatalog();
		else
			ShowMain();
	}

	public void RefreshCatalog() => RequestCatalog();

	public void ReceiveCatalogQuote(MerchantCatalogQuote quote)
	{
		if (_intents is null
			|| quote.Catalog != _catalog
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
		if (!_intents.TryFetchMerchantCatalog(_endpoint, _catalog, ship.Clone()))
			ShowMain();
	}

	private void ShowMain()
	{
		var tier = MerchantCatalog.Tier(_run.StarSystem.Map);
		_shell.SetTitle(_title);
		_shell.SetSubtitle(
			_catalog == EMerchantCatalog.Healing
				? _subtitle
				: $"{_subtitle} · {MerchantOfferDisplay.BandLabel(tier)}");
		_shell.SetHeader(HudHeaderMode.Close);
		_shell.SetBackHandler(null);
		_shell.SetFooter([]);

		var body = HudWidgets.CreateCardList();
		body.AddThemeConstantOverride("separation", 18);

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
				_catalog == EMerchantCatalog.Healing
					? "Loading services…"
					: "Loading shop catalog…"));
			_shell.SetBody(body);
			return;
		}

		if (_catalog == EMerchantCatalog.Healing)
			PopulateHealingOffers(body, ship);
		else
			PopulateUpgradeOffers(body, ship);

		_shell.SetBody(body);
	}

	private void PopulateHealingOffers(VBoxContainer body, ShipInstance ship)
	{
		var offers = _catalogQuote!.Offers;
		foreach (var offer in offers)
		{
			var captured = offer;
			var target = captured.Targets.Single();
			var canAfford = _run.StarSystem.Map.PlayerResources.CanApply(captured.Cost.Negate());
			body.AddChild(MerchantUiListItem.Create(
				HealingTitle(captured, target),
				HealingBody(captured, target, ship),
				captured.Cost,
				canAfford,
				() => PurchaseRequested?.Invoke(captured, target, ship.Id)));
		}

		if (offers.Count == 0)
			body.AddChild(HudWidgets.CreateStatusPanel(
				HudStatusKind.Neutral,
				"No support services are currently needed."));
	}

	private void PopulateUpgradeOffers(VBoxContainer body, ShipInstance ship)
	{
		var offers = _catalogQuote!.Offers;
		if (offers.Count == 0)
			body.AddChild(HudWidgets.CreateStatusPanel(
				HudStatusKind.Neutral,
				"No upgrade packs are available at this shop tier."));

		foreach (var offer in offers)
		{
			var captured = offer;
			var canAfford = _run.StarSystem.Map.PlayerResources.CanApply(captured.Cost.Negate());
			body.AddChild(MerchantUiListItem.Create(
				MerchantOfferDisplay.PackTitle(captured.Offering),
				$"{captured.Targets.Count} eligible targets.",
				captured.Cost,
				canAfford,
				() => ShowTargets(captured, ship)));
		}
	}

	private void ShowTargets(MerchantCatalog.Offer offer, ShipInstance ship)
	{
		if (_catalogQuote is null)
			return;

		_shell.SetTitle(MerchantOfferDisplay.PackTitle(offer.Offering));
		_shell.SetSubtitle("Choose a target");
		_shell.SetHeader(HudHeaderMode.Back, ShowMain);
		_shell.SetBackHandler(ShowMain);
		_shell.SetFooter([]);

		var body = HudWidgets.CreateCardList();
		foreach (var target in offer.Targets)
		{
			var captured = target;
			if (target.Mount is { } mount)
			{
				var installed = ship.Loadout.InstalledAbilities.Single(
					ability => ability.Mount == mount);
				var canAfford = _run.StarSystem.Map.PlayerResources.CanApply(offer.Cost.Negate());
				body.AddChild(MerchantUiListItem.Create(
					$"{MerchantOfferDisplay.FaceLabel(mount.Facet)} {MerchantOfferDisplay.KindLabel(mount.Kind)}",
					MerchantOfferDisplay.AbilityUpgradeBody(offer.Offering, installed),
					offer.Cost,
					canAfford,
					() => PurchaseRequested?.Invoke(offer, captured, ship.Id)));
				continue;
			}

			if (target.Face is { } face)
			{
				var canAfford = _run.StarSystem.Map.PlayerResources.CanApply(offer.Cost.Negate());
				body.AddChild(MerchantUiListItem.Create(
					MerchantOfferDisplay.FaceLabel(face),
					MerchantOfferDisplay.ShieldUpgradeBody(
						ship,
						face,
						offer.Offering.TargetLevel),
					offer.Cost,
					canAfford,
					() => PurchaseRequested?.Invoke(offer, captured, ship.Id)));
			}
		}

		_shell.SetBody(body);
	}

	private static string HealingTitle(
		MerchantCatalog.Offer offer,
		MerchantCatalog.Target target) =>
		offer.Offering.Kind switch
		{
			MerchantCatalog.Kind.RepairHull => "Repair hull",
			MerchantCatalog.Kind.RechargeAllShields => "Recharge all shields",
			MerchantCatalog.Kind.RechargeShieldFace when target.Face is { } face =>
				$"Recharge {MerchantOfferDisplay.FaceLabel(face).ToLowerInvariant()} shields",
			MerchantCatalog.Kind.BuyFuel => $"Buy fuel ({StarMarketSeed.FuelPurchaseAmount})",
			_ => throw new InvalidOperationException(
				$"Unsupported healing service {offer.Offering.Kind}."),
		};

	private static string HealingBody(
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
			MerchantCatalog.Kind.BuyFuel =>
				$"Purchase a {StarMarketSeed.FuelPurchaseAmount}-unit fuel bundle for the fleet.",
			_ => throw new InvalidOperationException(
				$"Unsupported healing service {offer.Offering.Kind}."),
		};

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
