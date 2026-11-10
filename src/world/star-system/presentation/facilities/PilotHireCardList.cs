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

internal static class PilotHireCardList
{
	public static VBoxContainer Build(State run, string contractId, Action<int> onHire)
	{
		var map = run.StarSystem.Map;
		var body = HudWidgets.CreateCardList();
		body.AddThemeConstantOverride("separation", 10);

		IReadOnlyList<PilotRecruitment.PilotHireCandidate> candidates;
		try
		{
			var purchased = PurchasedSlots(map, contractId);
			candidates = PilotRecruitment.RollAvailableCandidates(map, contractId, purchased);
		}
		catch (InvalidOperationException error)
		{
			GD.PushError($"Could not create pilot hire offers: {error}");
			return body;
		}

		foreach (var candidate in candidates)
		{
			var captured = candidate;
			var cost = PilotRecruitment.Price(map, captured);
			var canAfford = run.PilotRegistry.CanRegister && map.PlayerResources.CanApply(cost.Negate());
			body.AddChild(BuildCandidateCard(captured, cost, canAfford, onHire));
		}

		return body;
	}

	private static UiListItem BuildCandidateCard(
		PilotRecruitment.PilotHireCandidate candidate,
		ResourceBundle cost,
		bool canAfford,
		Action<int> onHire)
	{
		var profile = candidate.Profile;
		var item = new UiImageListItem(
			profile.Name,
			"",
			"",
			ShipPilotPortraitCatalog.TextureFor(profile.PortraitId))
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		item.SetDetailContent(PilotIdentityIconRail.BuildFor(profile));
		item.SetTrailingContent(BuildHireAndSalaryCost(cost, profile));
		item.SetAffordable(canAfford);
		item.SetEnabled(canAfford);
		if (canAfford)
			item.Pressed += () => onHire(candidate.Slot);

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
