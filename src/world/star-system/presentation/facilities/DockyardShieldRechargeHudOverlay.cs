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
		AppendUpgradeCards(body, ship, offers);
		body.AddChild(CreateHullRepairPanel(ship, offers));
		body.AddChild(CreateShieldRechargePanel(ship, offers));

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

	private void AppendUpgradeCards(
		VBoxContainer body,
		ShipInstance ship,
		IReadOnlyList<MerchantCatalog.Offer> offers)
	{
		foreach (var offer in offers)
		{
			if (offer.Offering.Kind is not (
				MerchantCatalog.Kind.UpgradeMaxShields or MerchantCatalog.Kind.UpgradeMaxHull))
				continue;

			var captured = offer;
			var shipId = ship.Id;
			var title = offer.Offering.Kind switch
			{
				MerchantCatalog.Kind.UpgradeMaxShields => MerchantOfferDisplay.ShieldUpgradeTitle(ship.Spec),
				MerchantCatalog.Kind.UpgradeMaxHull => MerchantOfferDisplay.HullUpgradeTitle(ship.Spec),
				_ => "Upgrade",
			};
			body.AddChild(HudWidgets.CreateCard(
				title,
				[ResourceCostDisplay.CreateMetadataRow(captured.Cost, string.Empty)],
				() => SupportPurchaseRequested?.Invoke(captured.Offering, shipId)));
		}
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
					$"Restore {missing} hull to {ship.Spec.MaxHullPoints}"),
			],
			() => SupportPurchaseRequested?.Invoke(repairOffer.Offering, shipId)));

		return panel;
	}

	private Control CreateShieldRechargePanel(ShipInstance ship, IReadOnlyList<MerchantCatalog.Offer> offers)
	{
		var panel = CreateStatusPanelShell(out var column);

		if (ship.Spec.MaxShieldPoints.MaxOnAnyFace <= 0)
		{
			column.AddChild(FullStatusLabel("This ship has no shield facings."));
			return panel;
		}

		var creditsOnHand = _map.PlayerResources.GetBalance(ResourceId.Credits);
		var shipId = ship.Id;
		var addedRow = false;

		foreach (var face in Faces)
		{
			if (ship.Spec.MaxShieldPoints[face] <= 0)
				continue;

			var faceOffer = offers.FirstOrDefault(o =>
				o.Offering.Kind == MerchantCatalog.Kind.RechargeShieldFace
				&& o.Offering.Face == face);
			if (faceOffer is null)
				continue;

			if (addedRow)
				column.AddChild(CreateRowDivider());

			column.AddChild(CreateFaceRow(ship, face, creditsOnHand, shipId, faceOffer));
			addedRow = true;
		}

		if (addedRow)
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
		MerchantCatalog.Offer faceOffer)
	{
		var max = ship.Spec.MaxShieldPoints[face];
		var current = System.Math.Clamp(ship.ShieldPoints[face], 0, max);
		var creditCost = CreditAmount(faceOffer.Cost);

		var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		row.AddThemeConstantOverride("separation", 10);

		var name = new Label
		{
			Text = ShortFaceName(face),
			CustomMinimumSize = new Vector2(72f, 0f),
		};
		HudStyles.ApplyTextRole(name, HudTextRole.Metadata);
		row.AddChild(name);

		var barHost = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		barHost.AddThemeConstantOverride("separation", CompactMetrics.Separation);
		row.AddChild(barHost);
		var blocks = new List<Panel>();
		ShieldBlockVisuals.SyncBlocks(barHost, blocks, CompactMetrics, max, current);

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
