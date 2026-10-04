using Godot;
using GrimSpace.World.StarSystem.Presentation.Ui;
using GrimSpace.Components;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed partial class DockyardHudOverlay : Control
{
	private readonly ModalShell _shell;
	private State _run = null!;
	private string _facilityTitle = "";
	private HudStatusKind? _statusKind;
	private string _statusMessage = "";
	private ESpatialOrientation? _selectedFace;
	private AbilityMount? _selectedMount;

	public event Action<MerchantCatalog.Offering, string>? PurchaseRequested;
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
		_selectedFace = null;
		_selectedMount = null;
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
		_shell.SetSubtitle("Abilities & upgrades");
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

		var offers = WeaponsCatalog.ListFor(ship).ToArray();
		var abilities = MerchantOfferDisplay.AbilitiesFor(ship, offers);
		var faces = abilities
			.Select(ability => ability.Mount.Facet)
			.Distinct()
			.OrderBy(face => face)
			.ToArray();
		if (faces.Length == 0)
		{
			body.AddChild(HudWidgets.CreateStatusPanel(
				HudStatusKind.Neutral,
				"No abilities available."));
			_shell.SetBody(body);
			return;
		}

		if (_selectedFace is not { } selected || !faces.Contains(selected))
			_selectedFace = faces[0];

		body.AddChild(CreateSelectorHeading("1 / Mount orientation"));
		var tabs = new TabBar
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(0, 40),
		};
		foreach (var face in faces)
			tabs.AddTab(FaceLabel(face));
		tabs.CurrentTab = Array.IndexOf(faces, _selectedFace.Value);
		body.AddChild(tabs);
		body.AddChild(CreateSelectorHeading("2 / Ability"));

		var abilityList = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		abilityList.AddThemeConstantOverride("h_separation", 10);
		abilityList.AddThemeConstantOverride("v_separation", 10);
		body.AddChild(abilityList);
		var offerList = HudWidgets.CreateCardList();
		body.AddChild(offerList);

		tabs.TabChanged += index =>
		{
			_selectedFace = faces[index];
			_selectedMount = null;
			ShowAbilities(abilityList, offerList, ship, abilities);
		};
		ShowAbilities(abilityList, offerList, ship, abilities);
		_shell.SetBody(body);
	}

	private void ShowAbilities(
		HFlowContainer abilityList,
		VBoxContainer offerList,
		ShipInstance ship,
		IReadOnlyList<MerchantOfferDisplay.AbilityEntry> abilities)
	{
		foreach (var child in abilityList.GetChildren())
		{
			abilityList.RemoveChild(child);
			child.QueueFree();
		}

		var faceAbilities = abilities.Where(ability => ability.Mount.Facet == _selectedFace).ToArray();
		var selected = faceAbilities.FirstOrDefault(ability => ability.Mount == _selectedMount)
			?? faceAbilities.First();
		_selectedMount = selected.Mount;
		var group = new ButtonGroup();

		foreach (var ability in faceAbilities)
		{
			var name = MerchantOfferDisplay.KindLabel(ability.Mount.Kind);
			var status = ability.IsInstalled ? "Installed" : "Available to install";
			var button = new Button
			{
				Text = $"{name}\n{status}",
				TooltipText = $"{FaceLabel(ability.Mount.Facet)} / {name} / {status}",
				Icon = SvgIconLoader.Load(
					MerchantOfferDisplay.AbilityIconPath(ability.Mount.Kind), HudStyles.AccentCyan, 36),
				IconAlignment = HorizontalAlignment.Center,
				VerticalIconAlignment = VerticalAlignment.Top,
				CustomMinimumSize = new Vector2(190, 112),
				ToggleMode = true,
				ButtonGroup = group,
				ButtonPressed = ability.Mount == _selectedMount,
				MouseDefaultCursorShape = CursorShape.PointingHand,
			};
			HudStyles.StyleButton(button, HudActionKind.Secondary);
			button.Toggled += pressed =>
			{
				if (!pressed)
					return;

				_selectedMount = ability.Mount;
				ShowOffers(offerList, ship, ability);
			};
			abilityList.AddChild(button);
		}

		ShowOffers(offerList, ship, selected);
	}

	private void ShowOffers(VBoxContainer body, ShipInstance ship, MerchantOfferDisplay.AbilityEntry ability)
	{
		foreach (var child in body.GetChildren())
		{
			body.RemoveChild(child);
			child.QueueFree();
		}

		body.AddChild(HudWidgets.CreateStatusPanel(
			HudStatusKind.Neutral,
			$"{MerchantOfferDisplay.KindLabel(ability.Mount.Kind)} / "
				+ (ability.IsInstalled ? "Installed" : "Available to install")));

		if (ability.Offers.Count == 0)
			body.AddChild(HudWidgets.CreateStatusPanel(HudStatusKind.Neutral, "No upgrades available for this ability."));

		foreach (var offer in ability.Offers)
		{
			var captured = offer;
			var shipId = ship.Id;
			body.AddChild(HudWidgets.CreateCard(
				TitleFor(captured, ship),
				[ResourceCostDisplay.CreateMetadataRow(captured.Cost, BodyFor(captured, ship))],
				() => PurchaseRequested?.Invoke(captured.Offering, shipId)));
		}
	}

	private static Label CreateSelectorHeading(string text) =>
		new()
		{
			Text = text,
			ThemeTypeVariation = "SectionHeading",
		};

	private static string FaceLabel(ESpatialOrientation face) =>
		face switch
		{
			ESpatialOrientation.Forward => "Forward",
			ESpatialOrientation.Retro => "Aft",
			ESpatialOrientation.Port => "Port",
			ESpatialOrientation.Starboard => "Starboard",
			ESpatialOrientation.Dorsal => "Dorsal",
			ESpatialOrientation.Ventral => "Ventral",
			_ => face.ToString(),
		};

	private bool TryGetActiveShip(out ShipInstance ship)
	{
		ship = null!;
		var shipId = _run.PlayerParty.ShipIds.FirstOrDefault();
		if (shipId is null)
			return false;

		ship = _run.ShipRegistry.Get(shipId);
		return true;
	}

	private static string TitleFor(MerchantCatalog.Offer offer, ShipInstance ship) =>
		offer.Offering.Kind switch
		{
			MerchantCatalog.Kind.InstallWeapon when offer.Offering.Mount is { } mount =>
				MerchantOfferDisplay.InstallTitle(mount),
			MerchantCatalog.Kind.UpgradeDamage when offer.Offering.Mount is { } mount =>
				MerchantOfferDisplay.DamageUpgradeTitle(
					mount,
					ship.Loadout.InstalledAbilities.First(a => a.Mount == mount).Spec),
			MerchantCatalog.Kind.UpgradeRange when offer.Offering.Mount is { } mount =>
				MerchantOfferDisplay.RangeUpgradeTitle(
					mount,
					ship.Loadout.InstalledAbilities.First(a => a.Mount == mount).Spec),
			_ => "Upgrade",
		};

	private static string BodyFor(MerchantCatalog.Offer offer, ShipInstance ship) =>
		offer.Offering.Kind switch
		{
			MerchantCatalog.Kind.InstallWeapon when offer.Offering.Mount is { } mount =>
				MerchantOfferDisplay.InstallBody(ship.Spec.BaselineFor(mount)),
			MerchantCatalog.Kind.UpgradeDamage when offer.Offering.Mount is { } mount =>
				MerchantOfferDisplay.DamageUpgradeBody(
					ship.Loadout.InstalledAbilities.First(a => a.Mount == mount).Spec),
			MerchantCatalog.Kind.UpgradeRange when offer.Offering.Mount is { } mount =>
				MerchantOfferDisplay.RangeUpgradeBody(
					ship.Loadout.InstalledAbilities.First(a => a.Mount == mount).Spec),
			_ => string.Empty,
		};
}
