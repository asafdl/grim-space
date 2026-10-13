using Godot;
using GrimSpace.Components;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Presentation.Ui;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed partial class HealingStationHudOverlay : Control
{
	private readonly ModalShell _shell;
	private State _run = null!;
	private string _facilityTitle = "";
	private HudStatusKind? _statusKind;
	private string _statusMessage = "";

	public event Action<MerchantCatalog.Offering, MerchantCatalog.Target, string>? HealingPurchaseRequested;
	public event Action? Closed;

	public HealingStationHudOverlay()
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
		_shell.Open("Healing station", facilityTitle);
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
		_shell.SetTitle("Healing station");
		_shell.SetSubtitle(_facilityTitle);
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

		var offers = MerchantCatalog.ListFor(EMerchantCatalog.Healing, ship, _run.StarSystem.Map);
		foreach (var offer in offers)
		{
			var captured = offer;
			var target = captured.Targets.Single();
			body.AddChild(HudWidgets.CreateCard(
				ServiceTitle(captured, target),
				[ResourceCostDisplay.CreateOfferDetails(
					ServiceBody(captured, target, ship),
					captured.Cost)],
				() => HealingPurchaseRequested?.Invoke(
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
