using Godot;
using GrimSpace.Components;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Ui.Components;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Presentation.Ui;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed partial class PilotHiringHudOverlay : Control
{
	private readonly ModalShell _shell;
	private State _run = null!;
	private string _facilityTitle = "";
	private string _contractId = "";

	public event Action<int>? HireRequested;
	public event Action? Closed;

	public PilotHiringHudOverlay()
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

	public void Open(State run, string facilityTitle, string contractId)
	{
		ArgumentException.ThrowIfNullOrEmpty(contractId);
		_run = run;
		_facilityTitle = facilityTitle;
		_contractId = contractId;
		_shell.Open("Pilot broker", _facilityTitle);
		ShowMain();
	}

	public void Sync(State run, string contractId)
	{
		ArgumentException.ThrowIfNullOrEmpty(contractId);
		_run = run;
		_contractId = contractId;
		if (IsOpen)
			ShowMain();
	}

	private void ShowMain()
	{
		var map = _run.StarSystem.Map;
		var tier = MerchantCatalog.Tier(map);
		_shell.SetTitle("Pilot broker");
		_shell.SetSubtitle($"{_facilityTitle} · {MerchantOfferDisplay.BandLabel(tier)}");
		_shell.SetHeader(HudHeaderMode.Close);
		_shell.SetBackHandler(null);
		_shell.SetFooter([]);

		var body = HudWidgets.CreateCardList();
		body.AddThemeConstantOverride("separation", 10);

		IReadOnlyList<PilotRecruitment.PilotHireCandidate> candidates;
		try
		{
			var purchased = PurchasedSlots(map, _contractId);
			candidates = PilotRecruitment.RollAvailableCandidates(map, _contractId, purchased);
		}
		catch (InvalidOperationException error)
		{
			body.AddChild(HudWidgets.CreateStatusPanel(HudStatusKind.Neutral, error.Message));
			_shell.SetBody(body);
			return;
		}

		if (candidates.Count == 0)
		{
			body.AddChild(HudWidgets.CreateStatusPanel(
				HudStatusKind.Neutral,
				"All pilot hire slots on this lead have been filled."));
			_shell.SetBody(body);
			return;
		}

		foreach (var candidate in candidates)
		{
			var captured = candidate;
			var cost = PilotRecruitment.Price(map, captured);
			var canAfford = map.PlayerResources.CanApply(cost.Negate());
			body.AddChild(BuildCandidateCard(captured, cost, canAfford));
		}

		_shell.SetBody(body);
	}

	private UiListItem BuildCandidateCard(
		PilotRecruitment.PilotHireCandidate candidate,
		ResourceBundle cost,
		bool canAfford)
	{
		var profile = candidate.Profile;
		var background = PilotBackgroundCatalog.Get(profile.BackgroundId);
		var metadata = $"{PilotHireDisplay.CardTitle(profile.BackgroundId)}\n"
			+ PilotHireDisplay.ShipPackageLine(candidate.Ship.Chassis, candidate.Ship.GearTier);
		var item = new UiImageListItem(
			profile.Name,
			metadata,
			"",
			ShipPilotPortraitCatalog.TextureFor(profile.PortraitId))
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		HudTooltipCard.SetTooltip(item, background.Description);
		item.SetTrailingContent(BuildHireAndSalaryCost(cost, profile));
		item.SetAffordable(canAfford);
		item.SetEnabled(canAfford);
		if (canAfford)
			item.Pressed += () => HireRequested?.Invoke(candidate.Slot);

		return item;
	}

	private static Control BuildHireAndSalaryCost(
		ResourceBundle hireCost,
		PilotRecruitProfile profile)
	{
		var row = new HBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		row.AddThemeConstantOverride("separation", 12);
		row.AddChild(ResourceCostDisplay.CreateBundleInline(hireCost));

		row.AddChild(ResourceCostDisplay.CreateAmountWithBadgeIcon(
			PilotHireDisplay.SalaryAmount(profile),
			EconomyIconCatalog.SalaryIconPath,
			EconomyIconCatalog.SalaryTooltip));
		row.AddChild(ResourceCostDisplay.CreateAmountWithBadgeIcon(
			StrategicEconomyConfig.HazardPayCreditsPerPilot,
			EconomyIconCatalog.HazardPayIconPath,
			EconomyIconCatalog.HazardPayTooltip));

		return row;
	}

	private static IReadOnlyCollection<int> PurchasedSlots(StarMap map, string contractId)
	{
		if (!map.ContractRegistry.TryGetState(contractId, out var state)
			|| state is not LeadContractState lead)
			return [];

		return lead.PurchasedPilotHireSlots;
	}

}
