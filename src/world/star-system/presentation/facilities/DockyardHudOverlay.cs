using Godot;
using GrimSpace.Components;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Presentation.Ui;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed partial class DockyardHudOverlay : Control
{
	private readonly ModalShell _shell;
	private State _run = null!;
	private string _facilityTitle = "";
	private HudStatusKind? _statusKind;
	private string _statusMessage = "";

	public event Action<MerchantCatalog.Offering, MerchantCatalog.Target, string>? PurchaseRequested;
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
		_shell.Open("Ability upgrades", facilityTitle);
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
		var tier = MerchantCatalog.Tier(_run.StarSystem.Map);
		_shell.SetTitle("Ability upgrades");
		_shell.SetSubtitle($"{_facilityTitle} · {MerchantOfferDisplay.BandLabel(tier)}");
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

		var offers = MerchantCatalog.ListFor(EMerchantCatalog.Weapons, ship, tier);
		if (offers.Count == 0)
			body.AddChild(HudWidgets.CreateStatusPanel(
				HudStatusKind.Neutral,
				"No ability upgrade packs are available at this shop tier."));

		foreach (var offer in offers)
		{
			var captured = offer;
			body.AddChild(HudWidgets.CreateCard(
				MerchantOfferDisplay.PackTitle(captured.Offering),
				[ResourceCostDisplay.CreateOfferDetails(
					$"{captured.Targets.Count} eligible installed abilities.",
					captured.Cost)],
				() => ShowTargets(captured, ship)));
		}

		_shell.SetBody(body);
	}

	private void ShowTargets(MerchantCatalog.Offer offer, ShipInstance ship)
	{
		_shell.SetTitle(MerchantOfferDisplay.PackTitle(offer.Offering));
		_shell.SetSubtitle("Choose an installed ability");
		_shell.SetHeader(HudHeaderMode.Back, ShowMain);
		_shell.SetBackHandler(ShowMain);
		_shell.SetFooter([]);

		var body = HudWidgets.CreateCardList();
		foreach (var target in offer.Targets)
		{
			if (target.Mount is not { } mount)
				continue;

			var installed = ship.Loadout.InstalledAbilities.Single(ability => ability.Mount == mount);
			var captured = target;
			body.AddChild(HudWidgets.CreateCard(
				$"{MerchantOfferDisplay.FaceLabel(mount.Facet)} {MerchantOfferDisplay.KindLabel(mount.Kind)}",
				[ResourceCostDisplay.CreateOfferDetails(
					MerchantOfferDisplay.AbilityUpgradeBody(
						offer.Offering,
						installed),
					offer.Cost)],
				() => PurchaseRequested?.Invoke(offer.Offering, captured, ship.Id)));
		}

		_shell.SetBody(body);
	}

	private void AddStatus(VBoxContainer body)
	{
		if (_statusKind is not null && !string.IsNullOrEmpty(_statusMessage))
			body.AddChild(HudWidgets.CreateStatusPanel(_statusKind.Value, _statusMessage));
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
}
