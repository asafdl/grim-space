using Godot;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Components;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Presentation.Scene;
using GrimSpace.Ui.Components;
using GrimSpace.World.StarSystem.Presentation.Ui;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed partial class CatalogMerchantHudOverlay : Control
{
	private readonly ModalShell _shell;
	private readonly ShipPicker _picker = new();
	private State _run = null!;
	private UserIntentTranslator? _intents;
	private MerchantEndpoint _endpoint;
	private EMerchantCatalog _catalog;
	private string _title = "";
	private string _subtitle = "";
	private readonly Dictionary<string, MerchantCatalogQuote> _quotes = new(StringComparer.Ordinal);
	private string? _selectedShipId;
	private HashSet<string> _requestedShipIds = new(StringComparer.Ordinal);
	private bool _requesting;

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
		AddChild(_picker);
	}

	public bool IsOpen => _shell.IsOpen;

	public bool TryHandleInput(InputEvent @event) => _shell.TryHandleInput(@event);

	public void Open(
		State run,
		UserIntentTranslator intents,
		MerchantEndpoint endpoint,
		EMerchantCatalog catalog,
		string title,
		string subtitle)
	{
		_run = run;
		_intents = intents;
		_endpoint = endpoint;
		_catalog = catalog;
		_title = title;
		_subtitle = subtitle;
		_quotes.Clear();
		_requestedShipIds.Clear();
		_selectedShipId = null;
		_shell.Open(title, subtitle);
		RequestCatalog();
	}

	public void Close()
	{
		_quotes.Clear();
		_shell.Close();
	}

	public void Sync(State run)
	{
		_run = run;
		if (!IsOpen)
			return;

		// Fetching itself raises WorldUpdated before quotes arrive; refetch only when ownership changes.
		if (_requesting || _requestedShipIds.SetEquals(OwnedShips().Select(ship => ship.Id)))
			ShowMain();
		else
			RequestCatalog();
	}

	public void RefreshCatalog() => RequestCatalog();

	public void ReceiveCatalogQuote(MerchantCatalogQuote quote)
	{
		if (_intents is null
			|| quote.Catalog != _catalog
			|| !quote.Endpoint.Equals(_endpoint)
			|| !_run.ItemInventory.IsOwnedBy(quote.ShipId, State.PlayerFleetUnitId))
			return;

		_quotes[quote.ShipId] = quote;
		if (IsOpen)
			ShowMain();
	}

	private IReadOnlyList<ShipInstance> OwnedShips() =>
		_run.ItemInventory.ShipsOwnedBy(State.PlayerFleetUnitId);

	private void RequestCatalog()
	{
		if (_intents is null || !IsOpen || _requesting)
			return;

		_requesting = true;
		try
		{
			_quotes.Clear();
			var ships = OwnedShips();
			_requestedShipIds = ships.Select(ship => ship.Id).ToHashSet(StringComparer.Ordinal);
			foreach (var ship in ships)
				_intents.TryFetchMerchantCatalog(_endpoint, _catalog, ship.Clone());
		}
		finally
		{
			_requesting = false;
		}
		ShowMain();
	}

	private void ShowMain()
	{
		_shell.SetTitle(_title);
		_shell.SetSubtitle(_subtitle);
		_shell.SetHeader(HudHeaderMode.Close);
		_shell.SetBackHandler(null);
		_shell.SetFooter([]);

		var body = HudWidgets.CreateCardList();
		body.AddThemeConstantOverride("separation", 18);
		var ships = OwnedShips();
		if (ships.Count > 0 && ships.All(ship => _quotes.ContainsKey(ship.Id)))
			PopulateRepairs(body, ships);

		_shell.SetBody(body);
	}

	private void PopulateRepairs(VBoxContainer body, IReadOnlyList<ShipInstance> ships)
	{
		var ordered = ships
			.OrderBy(HealthFraction)
			.ThenByDescending(ship => ShipLoadoutGearTier.FromLoadout(ship.Loadout, ship.Spec.Chassis))
			.ToArray();
		var damaged = ordered.Where(ship => RepairOffers(ship.Id).Count > 0).Select(ship => ship.Id).ToArray();
		if (_selectedShipId is null || !ships.Any(ship => ship.Id == _selectedShipId))
			_selectedShipId = damaged.FirstOrDefault() ?? ordered[0].Id;
		var selected = _run.ItemInventory.Get(_selectedShipId);

		var affordable = AffordableRepairs(ordered);
		var selectedCost = RepairCost([selected.Id]);
		body.AddChild(HealSelectedItem(selected, ordered, selectedCost));
		body.AddChild(BracketItem(
			"Heal all",
			"",
			RepairCost(affordable),
			affordable.Count > 0,
			() => RequestRepairs(affordable)));
		var fuel = _quotes[selected.Id].Offers.First(offer => offer.Offering.Kind == MerchantCatalog.Kind.BuyFuel);
		body.AddChild(BracketItem(
			$"Buy fuel ({StarMarketSeed.FuelPurchaseAmount})",
			"",
			fuel.Cost,
			CanAfford(fuel.Cost),
			() =>
			{
				_selectedShipId = null;
				PurchaseRequested?.Invoke(fuel, fuel.Target, selected.Id);
			}));
	}

	private static UiImageListItem BracketItem(
		string title,
		string metadata,
		ResourceBundle cost,
		bool canAfford,
		Action onPressed)
	{
		var item = new UiImageListItem(title, metadata, "", null)
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		item.SetTrailingContent(ResourceCostDisplay.CreateBundleInline(cost));
		item.SetAffordable(canAfford);
		item.SetEnabled(canAfford);
		item.Pressed += onPressed;
		return item;
	}

	/// <summary>Image row for the selected ship; right-click opens the ship picker.</summary>
	private UiImageListItem HealSelectedItem(
		ShipInstance selected,
		IReadOnlyList<ShipInstance> ordered,
		ResourceBundle cost)
	{
		var canHeal = RepairOffers(selected.Id).Count > 0 && CanAfford(cost);
		var item = new UiImageListItem(
			"Heal selected",
			$"{MerchantOfferDisplay.HullRepairBody(selected.CurrentHullPoints, selected.Loadout.BaseHullPoints)}\n"
			+ MerchantOfferDisplay.RechargeAllBody(selected.TotalCurrentShieldPoints, selected.TotalBaseShieldPoints),
			"",
			UnitPortraitCatalog.For(selected.Spec.Chassis)
				?? throw new InvalidOperationException($"No portrait for ship chassis {selected.Spec.Chassis}."),
			frameEdgeColor: ShipBox.TierColor(selected))
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		item.Portrait!.AddChild(new ShipHealthArcs(selected));
		item.SetTrailingContent(ResourceCostDisplay.CreateBundleInline(cost));
		item.SetTooltip(ShipRecruitmentDisplay.ShipTooltip(selected));
		item.SetAffordable(canHeal || RepairOffers(selected.Id).Count == 0);
		item.SetEnabled(canHeal);
		item.Pressed += () => RequestRepairs([selected.Id]);
		item.GuiInput += @event =>
		{
			if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right })
				return;
			item.AcceptEvent();
			_picker.Open(
				item.Portrait!,
				ordered.Where(ship => ship.Id != selected.Id),
				ShipRecruitmentDisplay.ShipTooltip,
				ship =>
				{
					_selectedShipId = ship.Id;
					ShowMain();
				});
		};
		return item;
	}

	private IReadOnlyList<MerchantCatalog.Offer> RepairOffers(string shipId) =>
		_quotes.TryGetValue(shipId, out var quote)
			? quote.Offers.Where(offer => offer.Offering.Kind
				is MerchantCatalog.Kind.RepairHull or MerchantCatalog.Kind.RechargeAllShields).ToArray()
			: [];

	private ResourceBundle RepairCost(IEnumerable<string> shipIds) =>
		ResourceBundle.Create(shipIds
			.SelectMany(RepairOffers)
			.SelectMany(offer => offer.Cost)
			.GroupBy(entry => entry.Key)
			.ToDictionary(group => group.Key, group => group.Sum(entry => entry.Value)));

	/// <summary>Ships in display order whose full repair still fits the remaining budget.</summary>
	private IReadOnlyList<string> AffordableRepairs(IReadOnlyList<ShipInstance> ships)
	{
		var picked = new List<string>();
		foreach (var ship in ships)
		{
			if (RepairOffers(ship.Id).Count == 0)
				continue;
			var next = RepairCost([.. picked, ship.Id]);
			if (!CanAfford(next))
				continue;
			picked.Add(ship.Id);
		}
		return picked;
	}

	/// <summary>Mean of hull and shield fractions, so large shield pools don't drown out hull damage.</summary>
	private static float HealthFraction(ShipInstance ship) =>
		(Fraction(ship.CurrentHullPoints, ship.Loadout.BaseHullPoints)
			+ Fraction(ship.TotalCurrentShieldPoints, ship.TotalBaseShieldPoints)) / 2f;

	private static float Fraction(int current, int max) => max <= 0 ? 1f : (float)current / max;

	private void RequestRepairs(IReadOnlyList<string> shipIds)
	{
		// Purchases refresh the view; drop the selection so it falls back to the most damaged ship.
		_selectedShipId = null;
		var purchases = shipIds
			.SelectMany(id => RepairOffers(id).Select(offer => (offer, id)))
			.ToArray();
		foreach (var (offer, id) in purchases)
			PurchaseRequested?.Invoke(offer, offer.Target, id);
	}

	private bool CanAfford(ResourceBundle cost) =>
		_run.StarSystem.Map.PlayerResources.CanApply(cost.Negate());
}
