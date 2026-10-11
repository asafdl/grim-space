using Godot;
using GrimSpace.Components;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Presentation.Ui;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed partial class DockyardShieldRechargeHudOverlay : Control
{
	private readonly ModalShell _shell;
	private State _run = null!;
	private string _facilityTitle = "";
	private HudStatusKind? _statusKind;
	private string _statusMessage = "";

	public event Action<MerchantCatalog.Offering, MerchantCatalog.Target, string>? SupportPurchaseRequested;
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
		_shell.Open("Ship support", facilityTitle);
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
		_shell.SetTitle("Ship support");
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

		var offers = MerchantCatalog.ListFor(EMerchantCatalog.ShipSupport, ship, tier);
		foreach (var offer in offers)
		{
			var captured = offer;
			if (IsUpgradePack(captured.Offering))
			{
				body.AddChild(HudWidgets.CreateCard(
					MerchantOfferDisplay.PackTitle(captured.Offering),
					[ResourceCostDisplay.CreateOfferDetails(
						$"{captured.Targets.Count} eligible shield faces.",
						captured.Cost)],
					() => ShowTargets(captured, ship)));
				continue;
			}

			var target = captured.Targets.Single();
			body.AddChild(HudWidgets.CreateCard(
				ServiceTitle(captured, target),
				[ResourceCostDisplay.CreateOfferDetails(
					ServiceBody(captured, target, ship),
					captured.Cost)],
				() => SupportPurchaseRequested?.Invoke(
					captured.Offering,
					target,
					ship.Id)));
		}

		if (offers.Count == 0)
			body.AddChild(HudWidgets.CreateStatusPanel(
				HudStatusKind.Neutral,
				"No support services are currently needed."));

		_shell.SetBody(body);
	}

	private void ShowTargets(MerchantCatalog.Offer offer, ShipInstance ship)
	{
		_shell.SetTitle(MerchantOfferDisplay.PackTitle(offer.Offering));
		_shell.SetSubtitle("Choose a shield face");
		_shell.SetHeader(HudHeaderMode.Back, ShowMain);
		_shell.SetBackHandler(ShowMain);
		_shell.SetFooter([]);

		var body = HudWidgets.CreateCardList();
		foreach (var target in offer.Targets)
		{
			if (target.Face is not { } face)
				continue;

			var captured = target;
			body.AddChild(HudWidgets.CreateCard(
				MerchantOfferDisplay.FaceLabel(face),
				[ResourceCostDisplay.CreateOfferDetails(
					MerchantOfferDisplay.ShieldUpgradeBody(
						ship,
						face,
						offer.Offering.TargetLevel),
					offer.Cost)],
				() => SupportPurchaseRequested?.Invoke(
					offer.Offering,
					captured,
					ship.Id)));
		}

		_shell.SetBody(body);
	}

	private static bool IsUpgradePack(MerchantCatalog.Offering offering) =>
		offering.Kind is MerchantCatalog.Kind.UpgradeMaxShields
			or MerchantCatalog.Kind.UpgradeDamage
			or MerchantCatalog.Kind.UpgradeRange;

	private static string ServiceTitle(
		MerchantCatalog.Offer offer,
		MerchantCatalog.Target target) =>
		offer.Offering.Kind switch
		{
			MerchantCatalog.Kind.RepairHull => "Repair hull",
			MerchantCatalog.Kind.RechargeAllShields => "Recharge all shields",
			MerchantCatalog.Kind.RechargeShieldFace when target.Face is { } face =>
				$"Recharge {MerchantOfferDisplay.FaceLabel(face).ToLowerInvariant()} shields",
			_ => throw new InvalidOperationException(
				$"Unsupported ship support service {offer.Offering.Kind}."),
		};

	private static string ServiceBody(
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
			_ => throw new InvalidOperationException(
				$"Unsupported ship support service {offer.Offering.Kind}."),
		};

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
