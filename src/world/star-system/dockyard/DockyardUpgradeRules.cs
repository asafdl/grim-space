namespace GrimSpace.World.StarSystem.Dockyard;

public static class DockyardUpgradeRules
{
	public const int MaxShieldUpgradeTier = 3;

	public static int ScrapForShieldUpgrade(int currentTier) => 35 + currentTier * 15;

	public static int ScrapForAbilityUpgrade(int currentTier) => 40 + currentTier * 20;

	public static string MkLabel(int upgradeTier) => $"Mk {upgradeTier + 1}";
}
