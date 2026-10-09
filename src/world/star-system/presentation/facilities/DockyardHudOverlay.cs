using Godot;
using GrimSpace.World.StarSystem.Presentation.Ui;
using GrimSpace.World.StarSystem;
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
		_shell.Open("Abilities & upgrades", _facilityTitle);
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
		_shell.SetTitle("Abilities & upgrades");
		_shell.SetSubtitle(_facilityTitle);
		_shell.SetHeader(HudHeaderMode.Close);
		_shell.SetBackHandler(null);
		_shell.SetFooter([]);

		var body = HudWidgets.CreateCardList();
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

		var tabs = new TabBar
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			ThemeTypeVariation = "MerchantTabBar",
		};
		foreach (var face in faces)
			tabs.AddTab(FaceLabel(face));
		tabs.CurrentTab = Array.IndexOf(faces, _selectedFace.Value);
		body.AddChild(tabs);

		var abilityList = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		abilityList.AddThemeConstantOverride("h_separation", 12);
		abilityList.AddThemeConstantOverride("v_separation", 12);
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
			var button = CreateAbilityButton(ability, group);
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

		if (ability.Offers.Count == 0)
			body.AddChild(HudWidgets.CreateStatusPanel(HudStatusKind.Neutral, "No upgrades available for this ability."));

		foreach (var offer in ability.Offers)
		{
			var captured = offer;
			var shipId = ship.Id;
			body.AddChild(HudWidgets.CreateCard(
				TitleFor(captured, ship),
				[ResourceCostDisplay.CreateOfferDetails(BodyFor(captured, ship), captured.Cost)],
				() => PurchaseRequested?.Invoke(captured.Offering, shipId)));
		}
	}

	private Button CreateAbilityButton(MerchantOfferDisplay.AbilityEntry ability, ButtonGroup group)
	{
		var name = MerchantOfferDisplay.KindLabel(ability.Mount.Kind);
		var status = ability.IsInstalled ? "Installed" : "Not installed";
		var button = new Button
		{
			TooltipText = $"{FaceLabel(ability.Mount.Facet)} / {name} / {status}",
			CustomMinimumSize = new Vector2(300, 76),
			ToggleMode = true,
			ButtonGroup = group,
			ButtonPressed = ability.Mount == _selectedMount,
			MouseDefaultCursorShape = CursorShape.PointingHand,
			ThemeTypeVariation = "AbilityChoiceButton",
		};

		var content = new HBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
		};
		content.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		content.OffsetLeft = 12;
		content.OffsetTop = 10;
		content.OffsetRight = -12;
		content.OffsetBottom = -10;
		content.AddThemeConstantOverride("separation", 12);
		button.AddChild(content);

		content.AddChild(new TextureRect
		{
			Texture = SvgIconLoader.Load(
				MerchantOfferDisplay.AbilityIconPath(ability.Mount.Kind), HudStyles.AccentCyan, 40),
			CustomMinimumSize = new Vector2(40, 40),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
			MouseFilter = MouseFilterEnum.Ignore,
		});
		var description = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		description.AddThemeConstantOverride("separation", 2);
		content.AddChild(description);
		description.AddChild(new Label
		{
			Text = name,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			ThemeTypeVariation = "AbilityNameLabel",
			MouseFilter = MouseFilterEnum.Ignore,
		});

		description.AddChild(new Label
		{
			Text = status,
			ThemeTypeVariation = ability.IsInstalled ? "AbilityInstalledLabel" : "AbilityAvailableLabel",
			MouseFilter = MouseFilterEnum.Ignore,
		});
		return button;
	}

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

	private static string TitleFor(MerchantCatalog.Offer offer, ShipInstance ship) =>
		offer.Offering.Kind switch
		{
			MerchantCatalog.Kind.InstallWeapon =>
				MerchantOfferDisplay.InstallTitle,
			MerchantCatalog.Kind.UpgradeDamage when offer.Offering.Mount is { } mount =>
				MerchantOfferDisplay.DamageUpgradeTitle(
					ship.Loadout.InstalledAbilities.First(a => a.Mount == mount).Spec),
			MerchantCatalog.Kind.UpgradeRange when offer.Offering.Mount is { } mount =>
				MerchantOfferDisplay.RangeUpgradeTitle(
					ship.Loadout.InstalledAbilities.First(a => a.Mount == mount).Spec),
			_ => "Upgrade",
		};

	private static string BodyFor(MerchantCatalog.Offer offer, ShipInstance ship) =>
		offer.Offering.Kind switch
		{
			MerchantCatalog.Kind.InstallWeapon =>
				MerchantOfferDisplay.InstallBody,
			MerchantCatalog.Kind.UpgradeDamage when offer.Offering.Mount is { } mount =>
				MerchantOfferDisplay.DamageUpgradeBody(
					ship.Loadout.InstalledAbilities.First(a => a.Mount == mount).Spec),
			MerchantCatalog.Kind.UpgradeRange when offer.Offering.Mount is { } mount =>
				MerchantOfferDisplay.RangeUpgradeBody(
					ship.Loadout.InstalledAbilities.First(a => a.Mount == mount).Spec),
			_ => string.Empty,
		};
}
