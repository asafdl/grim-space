using Godot;
using GrimSpace.World.StarSystem.Presentation.Ui;
using GrimSpace.Components;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Resources;
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

		if (!TryGetActiveShip(out var ship))
		{
			body.AddChild(HudWidgets.CreateStatusPanel(
				HudStatusKind.Neutral,
				"No ships in your party."));
			_shell.SetBody(body);
			return;
		}

		var offers = WeaponsCatalog.ListFor(ship).ToArray();
		var faces = offers
			.Where(offer => offer.Offering.Mount is not null)
			.Select(offer => offer.Offering.Mount!.Value.Facet)
			.Concat(ship.Spec.InstalledAbilities.Select(ability => ability.Mount.Facet))
			.Distinct()
			.OrderBy(face => face)
			.ToArray();
		if (faces.Length == 0)
		{
			body.AddChild(HudWidgets.CreateStatusPanel(
				HudStatusKind.Neutral,
				"No weapon offers available."));
			_shell.SetBody(body);
			return;
		}

		if (_selectedFace is not { } selected || !faces.Contains(selected))
			_selectedFace = faces[0];

		var tabs = new TabBar { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foreach (var face in faces)
			tabs.AddTab(FaceLabel(face));
		tabs.CurrentTab = Array.IndexOf(faces, _selectedFace.Value);
		tabs.TabChanged += index =>
		{
			_selectedFace = faces[index];
			ShowMain();
		};
		body.AddChild(tabs);

		var mounted = ship.Spec.InstalledAbilities
			.Where(ability => ability.Mount.Facet == _selectedFace)
			.ToArray();
		body.AddChild(HudWidgets.CreateStatusPanel(
			HudStatusKind.Neutral,
			mounted.Length == 0
				? "No weapon installed on this facet."
				: $"Installed: {string.Join(", ", mounted.Select(ability => ability.Spec.Kind.ToString()))}"));

		var faceOffers = offers.Where(offer => offer.Offering.Mount?.Facet == _selectedFace).ToArray();
		if (faceOffers.Length == 0)
			body.AddChild(HudWidgets.CreateStatusPanel(HudStatusKind.Neutral, "No upgrades available on this facet."));

		foreach (var offer in faceOffers)
		{
			var captured = offer;
			var shipId = ship.Id;
			body.AddChild(HudWidgets.CreateCard(
				TitleFor(captured, ship),
				[ResourceCostDisplay.CreateMetadataRow(captured.Cost, BodyFor(captured, ship))],
				() => PurchaseRequested?.Invoke(captured.Offering, shipId)));
		}

		_shell.SetBody(body);
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
					ship.Spec.InstalledAbilities.First(a => a.Mount == mount).Spec),
			MerchantCatalog.Kind.UpgradeRange when offer.Offering.Mount is { } mount =>
				MerchantOfferDisplay.RangeUpgradeTitle(
					mount,
					ship.Spec.InstalledAbilities.First(a => a.Mount == mount).Spec),
			_ => "Upgrade",
		};

	private static string BodyFor(MerchantCatalog.Offer offer, ShipInstance ship) =>
		offer.Offering.Kind switch
		{
			MerchantCatalog.Kind.InstallWeapon when offer.Offering.Mount is { } mount =>
				MerchantOfferDisplay.InstallBody(
					ShipCatalog.DefaultAbilitySpec(EType.Fighter, mount.Kind)!),
			MerchantCatalog.Kind.UpgradeDamage when offer.Offering.Mount is { } mount =>
				MerchantOfferDisplay.DamageUpgradeBody(
					ship.Spec.InstalledAbilities.First(a => a.Mount == mount).Spec),
			MerchantCatalog.Kind.UpgradeRange when offer.Offering.Mount is { } mount =>
				MerchantOfferDisplay.RangeUpgradeBody(
					ship.Spec.InstalledAbilities.First(a => a.Mount == mount).Spec),
			_ => string.Empty,
		};
}
