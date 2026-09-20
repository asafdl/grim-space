using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.World.StarSystem.Dockyard;

internal static class AbilitySpecUpgrade
{
	public static int Tier(AbilitySpec spec) =>
		spec switch
		{
			FlakSpec flak => flak.UpgradeTier,
			RailgunSpec railgun => railgun.UpgradeTier,
			PatrolBaySpec bay => bay.UpgradeTier,
			TorpedoLauncherSpec launcher => launcher.UpgradeTier,
			_ => 0,
		};
}
