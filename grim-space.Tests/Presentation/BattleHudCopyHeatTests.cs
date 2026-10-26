using GrimSpace.Battle.Effects;
using GrimSpace.Battle.PilotCapabilities;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Units.Heat;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Presentation;

public sealed class BattleHudCopyHeatTests
{
	[Fact]
	public void HeatTooltip_IncludesCostsAndStatus()
	{
		var tooltip = BattleHudCopy.HeatTooltip(40, HeatSpec.Standard, overdrive: false, overheat: false);

		Assert.Contains($"{HeatChangeEffect.PerApSpent} per AP", tooltip);
		Assert.Contains($"{HeatChangeEffect.PerMpSpent} per MP", tooltip);
		Assert.Contains($"{HeatChangeEffect.PerAbilityUse} per ability", tooltip);
		Assert.Contains($"{HeatSpec.Standard.ClearPerTurn} each round", tooltip);
		Assert.Contains($"+{OverdriveDamageBonusModifier.BonusPercent}%", tooltip);
		Assert.Contains("nominal", tooltip);
	}

	[Fact]
	public void AbilityTooltips_IncludeHeatCost()
	{
		Assert.Contains(BattleHudCopy.AbilityHeatCostLine, BattleHudCopy.ScrapDroneSwarmTooltipFor(ScrapDroneSwarmSpec.Baseline));
		Assert.Contains(BattleHudCopy.AbilityHeatCostLine, BattleHudCopy.LightningCannonTooltipFor(LightningCannonSpec.Baseline));
		Assert.Contains(BattleHudCopy.AbilityHeatCostLine, BattleHudCopy.GoopGunTooltipFor(GoopGunSpec.Baseline));
	}

	[Fact]
	public void ResourceTooltips_IncludeHeatCosts()
	{
		Assert.Contains($"{HeatChangeEffect.PerApSpent} heat", BattleHudCopy.ActionPointsTooltip);
		Assert.Contains($"{HeatChangeEffect.PerMpSpent} heat", BattleHudCopy.ManeuverPointsTooltip);
	}
}
