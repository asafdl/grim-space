using Godot;
using GrimSpace.World.StarSystem.Presentation.Ui;
using GrimSpace.World.StarSystem;
using GrimSpace.Components;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed partial class DockyardShieldRechargeHudOverlay : Control
{
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
	private string _facilityTitle = "";
	private HudStatusKind? _statusKind;
	private string _statusMessage = "";
	private ESpatialOrientation _selectedFace;

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
		_facilityTitle = facilityTitle;
		_statusKind = null;
		_statusMessage = "";
		_selectedFace = ESpatialOrientation.Forward;
		_shell.Open("Ship support", _facilityTitle);
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
		if (IsOpen)
			ShowMain();
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
		_shell.SetTitle("Ship support");
		_shell.SetSubtitle(_facilityTitle);
		_shell.SetHeader(HudHeaderMode.Close);
		_shell.SetBackHandler(null);
		_shell.SetFooter([]);

		var body = HudWidgets.CreateCardList();
		body.SizeFlagsVertical = SizeFlags.ExpandFill;
		body.AddThemeConstantOverride("separation", 18);

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
		body.AddChild(CreateHullSection(ship, offers));
		body.AddChild(CreateShieldSection(ship, offers));

		_shell.SetBody(body);
	}

	private bool TryGetActiveShip(out ShipInstance ship)
	{
		ship = null!;
		var runtime = _run.StarSystem.RuntimeFor(State.PlayerFleetUnitId);
		var shipId = PlayerFleetSelection.EnsureSelectedMember(
			_run.StarSystem.Map,
			runtime,
			State.PlayerFleetUnitId,
			_run.PlayerParty.ShipIds);
		if (shipId is null)
			return false;

		ship = _run.ShipRegistry.Get(shipId);
		return true;
	}

	private Control CreateHullSection(
		ShipInstance ship,
		IReadOnlyList<MerchantCatalog.Offer> offers)
	{
		var panel = CreateSupportSection("Hull", out var column);
		column.AddChild(FullStatusLabel($"Hull integrity {ship.HullPoints}/{ship.Loadout.MaxHullPoints}"));

		var repairOffer = offers.FirstOrDefault(o => o.Offering.Kind == MerchantCatalog.Kind.RepairHull);
		if (repairOffer is not null)
		{
			column.AddChild(HudWidgets.CreateCard(
				"Repair",
				[ResourceCostDisplay.CreateOfferDetails(
					MerchantOfferDisplay.HullRepairBody(ship.HullPoints, ship.Loadout.MaxHullPoints),
					repairOffer.Cost)],
				() => SupportPurchaseRequested?.Invoke(repairOffer.Offering, ship.Id)));
		}

		var upgradeOffer = offers.FirstOrDefault(o => o.Offering.Kind == MerchantCatalog.Kind.UpgradeMaxHull);
		if (upgradeOffer is null)
		{
			column.AddChild(FullStatusLabel("Hull capacity is fully upgraded."));
		}
		else
		{
			column.AddChild(HudWidgets.CreateCard(
				MerchantOfferDisplay.CapacityUpgradeTitle(ship.Loadout.HullUpgradeTier),
				[ResourceCostDisplay.CreateOfferDetails(
					MerchantOfferDisplay.HullCapacityBody(ship.Loadout.MaxHullPoints),
					upgradeOffer.Cost)],
				() => SupportPurchaseRequested?.Invoke(upgradeOffer.Offering, ship.Id)));
		}

		return panel;
	}

	private Control CreateShieldSection(ShipInstance ship, IReadOnlyList<MerchantCatalog.Offer> offers)
	{
		var panel = CreateSupportSection("Shields", out var column);
		var tabs = new TabBar
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			ThemeTypeVariation = "MerchantTabBar",
		};
		foreach (var face in Faces)
			tabs.AddTab(FaceLabel(face));
		tabs.CurrentTab = Array.IndexOf(Faces, _selectedFace);
		column.AddChild(tabs);

		var faceContent = HudWidgets.CreateCardList();
		column.AddChild(faceContent);
		tabs.TabChanged += index =>
		{
			_selectedFace = Faces[index];
			ShowShieldFace(faceContent, ship, offers);
		};
		ShowShieldFace(faceContent, ship, offers);

		var fillAllOffer = offers.FirstOrDefault(o =>
			o.Offering.Kind == MerchantCatalog.Kind.RechargeAllShields);
		if (fillAllOffer is not null)
		{
			column.AddChild(HudWidgets.CreateCard(
				"Recharge all",
				[ResourceCostDisplay.CreateOfferDetails(
					MerchantOfferDisplay.RechargeAllBody(
						ship.TotalCurrentShieldPoints,
						ship.TotalMaxShieldPoints),
					fillAllOffer.Cost)],
				() => SupportPurchaseRequested?.Invoke(fillAllOffer.Offering, ship.Id)));
		}

		return panel;
	}

	private void ShowShieldFace(
		VBoxContainer body,
		ShipInstance ship,
		IReadOnlyList<MerchantCatalog.Offer> offers)
	{
		foreach (var child in body.GetChildren())
		{
			body.RemoveChild(child);
			child.QueueFree();
		}

		body.AddChild(CreateFaceStatus(ship, _selectedFace));

		var rechargeOffer = offers.FirstOrDefault(o =>
			o.Offering.Kind == MerchantCatalog.Kind.RechargeShieldFace
				&& o.Offering.Face == _selectedFace);
		if (rechargeOffer is not null)
		{
			var max = ship.Loadout.MaxShieldPoints[_selectedFace];
			var current = System.Math.Clamp(ship.ShieldPoints[_selectedFace], 0, max);
			body.AddChild(HudWidgets.CreateCard(
				"Recharge",
				[ResourceCostDisplay.CreateOfferDetails(
					MerchantOfferDisplay.ShieldRechargeBody(current, max),
					rechargeOffer.Cost)],
				() => SupportPurchaseRequested?.Invoke(rechargeOffer.Offering, ship.Id)));
		}

		var upgradeOffer = offers.FirstOrDefault(o =>
			o.Offering.Kind == MerchantCatalog.Kind.UpgradeMaxShields
				&& o.Offering.Face == _selectedFace);
		if (upgradeOffer is null)
		{
			body.AddChild(FullStatusLabel($"{FaceLabel(_selectedFace)} capacity is fully upgraded."));
		}
		else
		{
			var max = ship.Loadout.MaxShieldPoints[_selectedFace];
			body.AddChild(HudWidgets.CreateCard(
				MerchantOfferDisplay.CapacityUpgradeTitle(
					ship.Loadout.ShieldUpgradeTiers[_selectedFace]),
				[ResourceCostDisplay.CreateOfferDetails(
					MerchantOfferDisplay.ShieldCapacityBody(max),
					upgradeOffer.Cost)],
				() => SupportPurchaseRequested?.Invoke(upgradeOffer.Offering, ship.Id)));
		}
	}

	private static Control CreateFaceStatus(ShipInstance ship, ESpatialOrientation face)
	{
		var max = ship.Loadout.MaxShieldPoints[face];
		var current = System.Math.Clamp(ship.ShieldPoints[face], 0, max);
		var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		row.AddThemeConstantOverride("separation", 10);

		var status = FullStatusLabel($"{FaceLabel(face)} shields {current}/{max}");
		status.CustomMinimumSize = new Vector2(180, 0);
		row.AddChild(status);
		var barHost = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		barHost.AddThemeConstantOverride("separation", CompactMetrics.Separation);
		row.AddChild(barHost);
		var blocks = new List<Panel>();
		ShieldBlockVisuals.SyncBlocks(barHost, blocks, CompactMetrics, max, current);
		return row;
	}

	private static PanelContainer CreateSupportSection(string title, out VBoxContainer column)
	{
		var panel = new PanelContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		HudStyles.SetPanelVariation(panel, "Status");

		var margin = new MarginContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		margin.AddThemeConstantOverride("margin_left", HudStyles.Margin);
		margin.AddThemeConstantOverride("margin_right", HudStyles.Margin);
		margin.AddThemeConstantOverride("margin_top", HudStyles.HalfMargin);
		margin.AddThemeConstantOverride("margin_bottom", HudStyles.HalfMargin);
		panel.AddChild(margin);

		column = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		column.AddThemeConstantOverride("separation", 8);
		margin.AddChild(column);
		column.AddChild(new Label
		{
			Text = title.ToUpperInvariant(),
			ThemeTypeVariation = HudStyles.InformativeSectionTitleLabelType,
		});
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

	private static string FaceLabel(ESpatialOrientation face) =>
		face switch
		{
			ESpatialOrientation.Forward => "Forward",
			ESpatialOrientation.Retro => "Aft",
			ESpatialOrientation.Starboard => "Starboard",
			ESpatialOrientation.Port => "Port",
			ESpatialOrientation.Dorsal => "Dorsal",
			ESpatialOrientation.Ventral => "Ventral",
			_ => face.ToString(),
		};
}
