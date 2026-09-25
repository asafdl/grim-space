using Godot;
using GrimSpace.World.StarSystem.Presentation.Ui;
using GrimSpace.Components;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed partial class DockyardShieldRechargeHudOverlay : Control
{
	private enum SupportTab { Hull, Shields, Capacity }

	private static readonly ESpatialOrientation[] Faces =
	[
		ESpatialOrientation.Forward,
		ESpatialOrientation.Retro,
		ESpatialOrientation.Starboard,
		ESpatialOrientation.Port,
		ESpatialOrientation.Dorsal,
		ESpatialOrientation.Ventral,
	];

	private static readonly ShieldBlockMetrics CompactMetrics = ShieldBlockMetrics.For(ShieldBlockBarSize.Compact);

	private readonly ModalShell _shell;
	private State _run = null!;
	private StarMap _map = null!;
	private string _facilityTitle = "";
	private HudStatusKind? _statusKind;
	private string _statusMessage = "";
	private SupportTab _selectedTab;

	public event Action<MerchantCatalog.Offering, string>? SupportPurchaseRequested;
	public event Action? Closed;

	public DockyardShieldRechargeHudOverlay()
	{
		AnchorsPreset = (int)LayoutPreset.FullRect;
		AnchorRight = 1f;
		AnchorBottom = 1f;
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;
		MouseFilter = MouseFilterEnum.Ignore;

		_shell = new ModalShell(HudThemeFamily.Informative);
		AddChild(_shell);
		_shell.SetDismissVisible(true);
		_shell.Closed += () => Closed?.Invoke();
	}

	public bool IsOpen => _shell.IsOpen;

	public void Open(State run, StarMap map, string facilityTitle)
	{
		_run = run;
		_map = map;
		_facilityTitle = facilityTitle;
		_statusKind = null;
		_statusMessage = "";
		_selectedTab = SupportTab.Hull;
		_shell.Open(_facilityTitle, string.Empty);
		ShowMain();
	}

	public void Close()
	{
		_statusKind = null;
		_statusMessage = "";
		_shell.Close();
	}

	public void Sync(State run, StarMap map)
	{
		_run = run;
		_map = map;
	}

	public void ShowError(string message)
	{
		_statusKind = HudStatusKind.Error;
		_statusMessage = message;
		ShowMain();
	}

	public void ShowConfirmation(string message, HudStatusKind kind)
	{
		_statusKind = kind;
		_statusMessage = message;
		ShowMain();
	}

	private void ShowMain()
	{
		_shell.SetTitle(_facilityTitle);
		_shell.SetSubtitle("Ship support");
		_shell.SetHeader(HudHeaderMode.Close);
		_shell.SetBackHandler(null);
		_shell.SetFooter([]);

		var body = HudWidgets.CreateCardList();

		if (_statusKind is not null && !string.IsNullOrEmpty(_statusMessage))
			body.AddChild(HudWidgets.CreateStatusPanel(_statusKind.Value, _statusMessage));

		if (!TryGetActiveShip(out var ship))
		{
			body.AddChild(HudWidgets.CreateStatusPanel(
				HudStatusKind.Neutral,
				"No ships in your party."));
			_shell.SetBody(body);
			return;
		}

		var offers = ShipSupportCatalog.ListFor(ship);
		var tabs = new TabBar { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foreach (var tab in Enum.GetValues<SupportTab>())
			tabs.AddTab(tab.ToString());
		tabs.CurrentTab = (int)_selectedTab;
		tabs.TabChanged += index =>
		{
			_selectedTab = (SupportTab)index;
			ShowMain();
		};
		body.AddChild(tabs);

		switch (_selectedTab)
		{
			case SupportTab.Hull:
				body.AddChild(CreateHullRepairPanel(ship, offers));
				break;
			case SupportTab.Shields:
				body.AddChild(CreateShieldRechargePanel(ship, offers));
				break;
			case SupportTab.Capacity:
				AppendHullUpgradeCard(body, ship, offers);
				break;
		}

		_shell.SetBody(body);
	}

	private bool TryGetActiveShip(out ShipInstance ship)
	{
		ship = null!;
		var shipId = _run.PlayerParty.ShipIds.FirstOrDefault();
		if (shipId is null)
			return false;

		ship = _run.ShipRegistry.Get(shipId);
		return true;
	}

	private void AppendHullUpgradeCard(
		VBoxContainer body,
		ShipInstance ship,
		IReadOnlyList<MerchantCatalog.Offer> offers)
	{
		var offer = offers.FirstOrDefault(o => o.Offering.Kind == MerchantCatalog.Kind.UpgradeMaxHull);
		if (offer is null)
		{
			body.AddChild(HudWidgets.CreateStatusPanel(HudStatusKind.Neutral, "Hull capacity is fully upgraded."));
			return;
		}

		body.AddChild(HudWidgets.CreateCard(
			MerchantOfferDisplay.HullUpgradeTitle(ship.Loadout),
			[ResourceCostDisplay.CreateMetadataRow(
				offer.Cost, $"Max hull {ship.Loadout.MaxHullPoints} -> {ship.Loadout.MaxHullPoints + 1}")],
			() => SupportPurchaseRequested?.Invoke(offer.Offering, ship.Id)));
	}

	private Control CreateHullRepairPanel(ShipInstance ship, IReadOnlyList<MerchantCatalog.Offer> offers)
	{
		var panel = CreateStatusPanelShell(out var column);
		var repairOffer = offers.FirstOrDefault(o => o.Offering.Kind == MerchantCatalog.Kind.RepairHull);

		if (repairOffer is null)
		{
			column.AddChild(FullStatusLabel("Hull integrity is full."));
			return panel;
		}

		var missing = ship.MissingHullPoints;
		var shipId = ship.Id;
		column.AddChild(HudWidgets.CreateCard(
			"Hull repair",
			[
				ResourceCostDisplay.CreateMetadataRow(
					repairOffer.Cost,
					$"Restore {missing} hull to {ship.Loadout.MaxHullPoints}"),
			],
			() => SupportPurchaseRequested?.Invoke(repairOffer.Offering, shipId)));

		return panel;
	}

	private Control CreateShieldRechargePanel(ShipInstance ship, IReadOnlyList<MerchantCatalog.Offer> offers)
	{
		var panel = CreateStatusPanelShell(out var column);

		var creditsOnHand = _map.PlayerResources.GetBalance(ResourceId.Credits);
		var shipId = ship.Id;

		foreach (var face in Faces)
		{
			var faceOffer = offers.FirstOrDefault(o =>
				o.Offering.Kind == MerchantCatalog.Kind.RechargeShieldFace
				&& o.Offering.Face == face);
			var upgradeOffer = offers.FirstOrDefault(o =>
				o.Offering.Kind == MerchantCatalog.Kind.UpgradeMaxShields
				&& o.Offering.Face == face);
			column.AddChild(CreateRowDivider());
			column.AddChild(CreateFaceRow(ship, face, creditsOnHand, shipId, faceOffer));
			if (upgradeOffer is not null)
			{
				column.AddChild(HudWidgets.CreateCard(
					MerchantOfferDisplay.ShieldUpgradeTitle(ship.Loadout, face),
					[ResourceCostDisplay.CreateMetadataRow(
						upgradeOffer.Cost,
						$"{ShortFaceName(face)} capacity {ship.Loadout.MaxShieldPoints[face]} -> {ship.Loadout.MaxShieldPoints[face] + 1}")],
					() => SupportPurchaseRequested?.Invoke(upgradeOffer.Offering, shipId)));
			}
			else
			{
				column.AddChild(FullStatusLabel($"{ShortFaceName(face)} capacity fully upgraded."));
			}
		}

		column.AddChild(CreateRowDivider());

		var fillAllOffer = offers.FirstOrDefault(o =>
			o.Offering.Kind == MerchantCatalog.Kind.RechargeAllShields);
		if (fillAllOffer is not null)
		{
			var fillAllCost = CreditAmount(fillAllOffer.Cost);
			column.AddChild(ResourceCostDisplay.CreateLabeledCostButton(
				"Fill all",
				ResourceId.Credits,
				fillAllCost,
				creditsOnHand >= fillAllCost,
				() => SupportPurchaseRequested?.Invoke(fillAllOffer.Offering, shipId)));
		}

		return panel;
	}

	private Control CreateFaceRow(
		ShipInstance ship,
		ESpatialOrientation face,
		int creditsOnHand,
		string shipId,
		MerchantCatalog.Offer? faceOffer)
	{
		var max = ship.Loadout.MaxShieldPoints[face];
		var current = System.Math.Clamp(ship.ShieldPoints[face], 0, max);
		var creditCost = faceOffer is null ? 0 : CreditAmount(faceOffer.Cost);

		var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		row.AddThemeConstantOverride("separation", 10);

		var name = new Label
		{
			Text = $"{ShortFaceName(face)} {current}/{max}",
			CustomMinimumSize = new Vector2(72f, 0f),
		};
		HudStyles.ApplyTextRole(name, HudTextRole.Metadata);
		row.AddChild(name);

		var barHost = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		barHost.AddThemeConstantOverride("separation", CompactMetrics.Separation);
		row.AddChild(barHost);
		var blocks = new List<Panel>();
		ShieldBlockVisuals.SyncBlocks(barHost, blocks, CompactMetrics, max, current);

		if (faceOffer is not null)
			row.AddChild(ResourceCostDisplay.CreateLabeledCostButton(
				"Fill",
				ResourceId.Credits,
				creditCost,
				creditCost > 0 && creditsOnHand >= creditCost,
				() => SupportPurchaseRequested?.Invoke(faceOffer.Offering, shipId)));

		return row;
	}

	private static PanelContainer CreateStatusPanelShell(out VBoxContainer column)
	{
		var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		HudStyles.SetPanelVariation(panel, "Status");

		var margin = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		margin.AddThemeConstantOverride("margin_left", HudStyles.Margin);
		margin.AddThemeConstantOverride("margin_right", HudStyles.Margin);
		margin.AddThemeConstantOverride("margin_top", HudStyles.HalfMargin);
		margin.AddThemeConstantOverride("margin_bottom", HudStyles.HalfMargin);
		panel.AddChild(margin);

		column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		column.AddThemeConstantOverride("separation", 8);
		margin.AddChild(column);
		return panel;
	}

	private static Label FullStatusLabel(string text)
	{
		var label = new Label
		{
			Text = text,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		HudStyles.ApplyTextRole(label, HudTextRole.Metadata);
		return label;
	}

	private static Control CreateRowDivider()
	{
		var padding = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		padding.AddThemeConstantOverride("margin_top", 2);
		padding.AddThemeConstantOverride("margin_bottom", 2);
		padding.AddChild(HudWidgets.CreateInformativeHairline());
		return padding;
	}

	private static int CreditAmount(ResourceBundle cost)
	{
		foreach (var (id, amount) in cost)
		{
			if (id == ResourceId.Credits)
				return amount;
		}

		return 0;
	}

	private static string ShortFaceName(ESpatialOrientation face) =>
		face switch
		{
			ESpatialOrientation.Forward => "fore",
			ESpatialOrientation.Retro => "aft",
			ESpatialOrientation.Starboard => "stbd",
			ESpatialOrientation.Port => "port",
			ESpatialOrientation.Dorsal => "dorsal",
			ESpatialOrientation.Ventral => "ventral",
			_ => face.ToString().ToLowerInvariant(),
		};
}
