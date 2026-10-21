using Godot;
using GrimSpace.Components;
using GrimSpace.Run;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.Ui.Components;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed partial class ShipRecruitmentHudOverlay : Control
{
	private readonly ModalShell _shell;
	private State _run = null!;
	private string _facilityTitle = "";

	public event Action<RecruitmentOffer>? RecruitmentRequested;
	public event Action? Closed;

	public ShipRecruitmentHudOverlay()
	{
		AnchorsPreset = (int)LayoutPreset.FullRect;
		AnchorRight = 1f;
		AnchorBottom = 1f;
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;
		MouseFilter = MouseFilterEnum.Ignore;

		_shell = new ModalShell(UiContainerVisual.Modal, HudThemeFamily.Informative);
		AddChild(_shell);
		_shell.SetDismissVisible(true);
		_shell.Closed += () => Closed?.Invoke();
	}

	public bool IsOpen => _shell.IsOpen;

	public bool TryHandleInput(InputEvent @event) => _shell.TryHandleInput(@event);

	public void Open(State run, string facilityTitle)
	{
		_run = run;
		_facilityTitle = facilityTitle;
		_shell.Open("Ship broker", _facilityTitle);
		ShowMain();
	}

	public void Sync(State run)
	{
		_run = run;
		if (IsOpen)
			ShowMain();
	}

	private void ShowMain()
	{
		var tier = MerchantCatalog.Tier(_run.StarSystem.Map);
		_shell.SetTitle("Ship broker");
		_shell.SetSubtitle($"{_facilityTitle} · {MerchantOfferDisplay.BandLabel(tier)}");
		_shell.SetHeader(HudHeaderMode.Close);
		_shell.SetBackHandler(null);
		_shell.SetFooter([]);

		var body = HudWidgets.CreateCardList();
		body.AddThemeConstantOverride("separation", 18);


		var ownedShipCount = _run.ItemInventory.ShipsOwnedBy(State.PlayerFleetUnitId).Count();
		body.AddChild(HudWidgets.CreateStatusPanel(
			HudStatusKind.Neutral,
			$"Owned ships: {ownedShipCount}. Purchased hulls join your reserve until a pilot is assigned."));

		var offers = _run.StarSystem.Map.Market.ListRecruitmentOffers(tier);
		if (offers.Count == 0)
			body.AddChild(HudWidgets.CreateStatusPanel(
				HudStatusKind.Neutral,
				"No ships are available at this shop tier."));

		foreach (var offer in offers)
		{
			var captured = offer;
			var canAfford = _run.StarSystem.Map.PlayerResources.CanApply(captured.Cost.Negate());
			var availability = canAfford
				? "Adds to your owned reserve (no pilot included)."
				: "You do not have the required resources.";
			body.AddChild(MerchantUiListItem.Create(
				DisplayName(captured.Chassis),
				availability,
				captured.Cost,
				canAfford,
				() => RecruitmentRequested?.Invoke(captured)));
		}

		_shell.SetBody(body);
	}

	private static string DisplayName(EType chassis) =>
		chassis switch
		{
			EType.RepurposedMiner => "Repurposed Miner",
			EType.Gunship => "Gunship",
			_ => chassis.ToString(),
		};
}
