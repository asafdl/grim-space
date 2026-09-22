using Godot;
using GrimSpace.World.StarSystem.Presentation.Ui;
using GrimSpace.Components;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Dockyard;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed partial class DockyardHudOverlay : Control
{
	private enum DockyardTab
	{
		Hull,
		Shields,
		Abilities,
	}

	private readonly ModalShell _shell;
	private State _run = null!;
	private string _facilityTitle = "";
	private DockyardTab _activeTab = DockyardTab.Shields;
	private HudStatusKind? _statusKind;
	private string _statusMessage = "";

	public event Action<string, string>? PurchaseRequested;
	public event Action<string>? HullRepairRequested;
	public event Action? Closed;

	public DockyardHudOverlay()
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
		_facilityTitle = facilityTitle;
		_activeTab = DockyardTab.Hull;
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

	public void Sync(State run, StarMap map) => _run = run;

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
		_shell.SetSubtitle(string.Empty);
		_shell.SetHeader(HudHeaderMode.Close);
		_shell.SetBackHandler(null);
		_shell.SetFooter([]);

		var body = HudWidgets.CreateCardList();

		if (_statusKind is not null && !string.IsNullOrEmpty(_statusMessage))
			body.AddChild(HudWidgets.CreateStatusPanel(_statusKind.Value, _statusMessage));

		body.AddChild(CreateTabBar());

		if (!TryGetActiveShip(out var ship))
		{
			body.AddChild(HudWidgets.CreateStatusPanel(
				HudStatusKind.Neutral,
				"No ships in your party."));
			_shell.SetBody(body);
			return;
		}

		switch (_activeTab)
		{
			case DockyardTab.Hull:
				AppendHullTab(body, ship);
				break;
			case DockyardTab.Shields:
				AppendShieldTab(body, ship);
				break;
			case DockyardTab.Abilities:
				AppendAbilitiesTab(body, ship);
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

	private Control CreateTabBar()
	{
		var row = new HBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		row.AddThemeConstantOverride("separation", 8);
		row.AddChild(CreateTabButton("Hull", DockyardTab.Hull));
		row.AddChild(CreateTabButton("Shields", DockyardTab.Shields));
		row.AddChild(CreateTabButton("Abilities", DockyardTab.Abilities));
		return row;
	}

	private Button CreateTabButton(string label, DockyardTab tab)
	{
		var button = new Button
		{
			Text = label,
			CustomMinimumSize = new Vector2(0, 44),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		HudStyles.StyleButton(button, _activeTab == tab ? HudActionKind.Primary : HudActionKind.Secondary);
		button.Pressed += () =>
		{
			_activeTab = tab;
			ShowMain();
		};
		return button;
	}

	private void AppendHullTab(VBoxContainer body, ShipInstance ship)
	{
		if (!DockyardHullRepair.TryQuote(ship, out var cost))
		{
			body.AddChild(HudWidgets.CreateStatusPanel(
				HudStatusKind.Neutral,
				"Hull integrity is full."));
			return;
		}

		var missing = DockyardHullRepair.MissingHull(ship);
		body.AddChild(HudWidgets.CreateCard(
			"Hull repair",
			[
				ResourceCostDisplay.CreateMetadataRow(
					cost,
					$"Restore {missing} hull to {ship.Spec.MaxHullPoints}"),
			],
			() => HullRepairRequested?.Invoke(ship.Id)));
	}

	private void AppendShieldTab(VBoxContainer body, ShipInstance ship)
	{
		var offers = DockyardOffers.ListFor(ship)
			.Where(offer => offer.Category == EDockyardUpgradeCategory.MaxShields)
			.ToArray();

		if (offers.Length == 0)
		{
			body.AddChild(HudWidgets.CreateStatusPanel(
				HudStatusKind.Neutral,
				"No shield upgrades available."));
			return;
		}

		AppendPurchaseCards(body, ship, offers);
	}

	private void AppendAbilitiesTab(VBoxContainer body, ShipInstance ship)
	{
		var offers = DockyardOffers.ListFor(ship)
			.Where(offer => offer.Category == EDockyardUpgradeCategory.Ability)
			.ToArray();

		if (offers.Length == 0)
		{
			body.AddChild(HudWidgets.CreateStatusPanel(
				HudStatusKind.Neutral,
				"No ability upgrades available."));
			return;
		}

		AppendPurchaseCards(body, ship, offers);
	}

	private void AppendPurchaseCards(VBoxContainer body, ShipInstance ship, IReadOnlyList<DockyardUpgradeOffer> offers)
	{
		foreach (var offer in offers)
		{
			var captured = offer;
			body.AddChild(HudWidgets.CreateCard(
				TitleFor(offer, ship),
				[ ResourceCostDisplay.CreateMetadataRow(captured.Cost, BodyFor(offer, ship)) ],
				() => PurchaseRequested?.Invoke(captured.Id, ship.Id)));
		}
	}

	private static string TitleFor(DockyardUpgradeOffer offer, ShipInstance ship) =>
		offer.Category switch
		{
			EDockyardUpgradeCategory.MaxShields => DockyardUpgradeDisplay.ShieldUpgradeTitle(ship.Spec),
			EDockyardUpgradeCategory.Ability when offer.Mount is { } mount
				&& offer.RequiredAbilitySpec is { } required =>
				DockyardUpgradeDisplay.AbilityUpgradeTitle(mount, required),
			_ => "Upgrade",
		};

	private static string BodyFor(DockyardUpgradeOffer offer, ShipInstance ship) =>
		offer.Category switch
		{
			EDockyardUpgradeCategory.MaxShields => "Raise max shields on all faces",
			EDockyardUpgradeCategory.Ability when offer.RequiredAbilitySpec is { } required =>
				DockyardUpgradeDisplay.AbilityUpgradeBody(required),
			_ => string.Empty,
		};

}
